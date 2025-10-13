using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
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

    public string[] GetNetworkAdapters()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n =>
                    // Only show physical network adapters that are up
                    n.OperationalStatus == OperationalStatus.Up &&
                    (n.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     n.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet) &&
                    // Exclude virtual adapters (VirtualBox, VMware, Hyper-V, etc.)
                    !n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                    !n.Description.Contains("VMware", StringComparison.OrdinalIgnoreCase) &&
                    !n.Description.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) &&
                    !n.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) &&
                    !n.Description.Contains("vEthernet", StringComparison.OrdinalIgnoreCase) &&
                    !n.Name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Name)
                .Distinct() // Remove any duplicates
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
                // Use netsh to change IP without showing command window
                var processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface ip set address name=\"{adapterName}\" static {ipAddress} {subnetMask} {gateway}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas" // Requires admin privileges
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    throw new Exception("Failed to start netsh process");

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new Exception($"netsh failed: {error}");
                }
            });

            // Wait a moment for the IP to be applied
            await Task.Delay(1000);

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
            var adapter = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.Name == adapterName);

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
