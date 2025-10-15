using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace PLCManager.Services;

public class NetworkService
{
    public async Task<bool> PingAsync(string ipAddress)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 2000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public string[] GetNetworkAdapters(bool includeInactive = false)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n =>
                {
                    // Check operational status
                    if (!includeInactive && n.OperationalStatus != OperationalStatus.Up)
                        return false;

                    // Only allow Ethernet and Wireless types
                    if (n.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                        n.NetworkInterfaceType != NetworkInterfaceType.GigabitEthernet)
                        return false;

                    // Exclude any virtual/loopback/tunnel adapters
                    var desc = n.Description.ToLowerInvariant();
                    var name = n.Name.ToLowerInvariant();

                    string[] excludeKeywords = new[]
                    {
                        "virtual", "vmware", "virtualbox", "hyper-v", "vethernet",
                        "loopback", "tunnel", "vpn", "tap", "tun", "wsl", "docker",
                        "vbox", "parallels", "pseudo", "miniport", "wan miniport",
                        "bluetooth", "isatap", "teredo", "6to4"
                    };

                    if (excludeKeywords.Any(keyword => desc.Contains(keyword) || name.Contains(keyword)))
                        return false;

                    // For active adapters, must have IPv4 address
                    if (!includeInactive)
                    {
                        var ipProps = n.GetIPProperties();
                        var hasIPv4 = ipProps.UnicastAddresses
                            .Any(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                     !System.Net.IPAddress.IsLoopback(a.Address));

                        if (!hasIPv4)
                            return false;
                    }

                    return true;
                })
                .Select(n =>
                {
                    var status = n.OperationalStatus == OperationalStatus.Up ? "" : " [INACTIVE]";
                    return $"{n.Name} ({n.Description}){status}";
                })
                .Distinct()
                .OrderBy(n => n)
                .ToArray();

            return interfaces;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task<(bool success, string message)> ChangeIpAddressAsync(
        string adapterName,
        string ipAddress,
        string subnetMask,
        string gateway)
    {
        try
        {
            await Task.Run(() =>
            {
                // Extract adapter name if it's in the format "Name (Description)" or "Name (Description) [INACTIVE]"
                string actualAdapterName = adapterName;
                if (adapterName.Contains("("))
                {
                    actualAdapterName = adapterName.Substring(0, adapterName.IndexOf("(")).Trim();
                }

                // Create a batch file to run netsh and capture result
                var batchPath = Path.Combine(Path.GetTempPath(), $"setip_{Guid.NewGuid()}.bat");
                var resultPath = Path.Combine(Path.GetTempPath(), $"setip_result_{Guid.NewGuid()}.txt");
                var outputPath = Path.Combine(Path.GetTempPath(), $"setip_output_{Guid.NewGuid()}.txt");

                try
                {
                    // Build netsh command - gateway is optional
                    string netshCmd;
                    if (string.IsNullOrWhiteSpace(gateway))
                    {
                        netshCmd = $"netsh interface ip set address name=\"{actualAdapterName}\" static {ipAddress} {subnetMask}";
                    }
                    else
                    {
                        netshCmd = $"netsh interface ip set address name=\"{actualAdapterName}\" static {ipAddress} {subnetMask} {gateway}";
                    }

                    // Write batch file that runs netsh and captures output and exit code
                    string batchContent = $@"@echo off
{netshCmd} > ""{outputPath}"" 2>&1
echo %ERRORLEVEL% > ""{resultPath}""
";
                    File.WriteAllText(batchPath, batchContent);

                    // Run batch file with admin privileges
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = batchPath,
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using var process = Process.Start(processInfo);
                    if (process == null)
                        throw new Exception("Failed to start elevation process. User may have cancelled.");

                    // Wait for process to complete
                    process.WaitForExit();

                    // Give it a moment to write the result files
                    Thread.Sleep(1500);

                    // Read output for debugging
                    string output = "";
                    if (File.Exists(outputPath))
                    {
                        output = File.ReadAllText(outputPath).Trim();
                    }

                    // Check if result file exists and read exit code
                    if (File.Exists(resultPath))
                    {
                        string resultText = File.ReadAllText(resultPath).Trim();
                        if (int.TryParse(resultText, out int exitCode) && exitCode != 0)
                        {
                            string errorMsg = $"netsh failed with exit code {exitCode}";
                            if (!string.IsNullOrWhiteSpace(output))
                            {
                                errorMsg += $"\nDetails: {output}";
                            }
                            errorMsg += $"\nAdapter: '{actualAdapterName}'\nCommand: {netshCmd}";
                            throw new Exception(errorMsg);
                        }
                    }
                }
                finally
                {
                    // Cleanup temp files
                    try
                    {
                        if (File.Exists(batchPath)) File.Delete(batchPath);
                        if (File.Exists(resultPath)) File.Delete(resultPath);
                        if (File.Exists(outputPath)) File.Delete(outputPath);
                    }
                    catch { }
                }
            });

            // Wait for IP to be applied
            await Task.Delay(1500);

            return (true, "IP address changed successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    public string GetCurrentIpAddress(string adapterName)
    {
        try
        {
            // Extract adapter name if it's in the format "Name (Description)" or "Name (Description) [INACTIVE]"
            string actualAdapterName = adapterName;
            if (adapterName.Contains("("))
            {
                actualAdapterName = adapterName.Substring(0, adapterName.IndexOf("(")).Trim();
            }

            var adapter = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.Name == actualAdapterName);

            if (adapter == null)
                return "Not found";

            var properties = adapter.GetIPProperties();
            var ipAddress = properties.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            return ipAddress?.Address.ToString() ?? "No IP";
        }
        catch
        {
            return "Error";
        }
    }
}
