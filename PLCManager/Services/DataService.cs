using PLCManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PLCManager.Services;

public class DataService
{
    private readonly string _defaultFilePath;

    public DataService()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NodusIP");

        Directory.CreateDirectory(appDataFolder);
        _defaultFilePath = Path.Combine(appDataFolder, "plc-connections.json");
    }

    public async Task SaveConnectionsAsync(IEnumerable<PlcConnection> connections, string? filePath = null)
    {
        try
        {
            var path = filePath ?? _defaultFilePath;
            var json = JsonSerializer.Serialize(connections, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save connections: {ex.Message}");
        }
    }

    public async Task<List<PlcConnection>> LoadConnectionsAsync(string? filePath = null)
    {
        try
        {
            var path = filePath ?? _defaultFilePath;

            if (!File.Exists(path))
                return new List<PlcConnection>();

            var json = await File.ReadAllTextAsync(path);
            var connections = JsonSerializer.Deserialize<List<PlcConnection>>(json);

            return connections ?? new List<PlcConnection>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load connections: {ex.Message}");
        }
    }
}
