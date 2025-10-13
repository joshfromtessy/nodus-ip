using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PLCManager.Models;
using PLCManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PLCManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NetworkService _networkService;
    private readonly DataService _dataService;

    public ObservableCollection<PlcConnection> Connections { get; } = new ObservableCollection<PlcConnection>
    {
        new PlcConnection
        {
            Name = "Sample PLC",
            PlcIpAddress = "192.168.1.100",
            MyIpAddress = "192.168.1.50",
            SubnetMask = "255.255.255.0",
            Gateway = "192.168.1.1",
            NetworkAdapter = "Ethernet",
            Status = "Not connected",
            Notes = "This is a sample connection"
        },
        new PlcConnection
        {
            Name = "Test PLC 2",
            PlcIpAddress = "10.0.0.50",
            MyIpAddress = "10.0.0.100",
            SubnetMask = "255.255.255.0",
            Gateway = "10.0.0.1",
            NetworkAdapter = "WiFi",
            Status = "Online",
            Notes = "Second test"
        }
    };

    [ObservableProperty]
    private PlcConnection? _selectedConnection;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<string> _availableAdapters = new ObservableCollection<string>();

    public ObservableCollection<string> Buildings { get; } = new ObservableCollection<string>
    {
        "West",
        "East",
        "South",
        "North",
        "Skaneateles",
        "Auburn"
    };

    public ObservableCollection<BuildingGroup> GroupedConnections
    {
        get
        {
            var groups = new ObservableCollection<BuildingGroup>();
            foreach (var building in Buildings)
            {
                var connectionsInBuilding = Connections.Where(c => c.Building == building).ToList();
                if (connectionsInBuilding.Any())
                {
                    groups.Add(new BuildingGroup
                    {
                        BuildingName = building,
                        Connections = connectionsInBuilding
                    });
                }
            }
            return groups;
        }
    }

    public MainWindowViewModel()
    {
        _networkService = new NetworkService();
        _dataService = new DataService();

        LoadAvailableAdapters();

        // DON'T load connections on startup - it clears the sample data
        // _ = LoadConnectionsAsync();
    }

    private void LoadAvailableAdapters()
    {
        var adapters = _networkService.GetNetworkAdapters();
        AvailableAdapters.Clear();
        foreach (var adapter in adapters)
        {
            AvailableAdapters.Add(adapter);
        }
    }

    [RelayCommand]
    private void SelectConnection(PlcConnection? connection)
    {
        if (connection != null)
        {
            SelectedConnection = connection;
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        var newConnection = new PlcConnection
        {
            Name = "New PLC",
            NetworkAdapter = AvailableAdapters.FirstOrDefault() ?? string.Empty,
            Status = "Not connected",
            Building = "West"
        };
        Connections.Add(newConnection);
        SelectedConnection = newConnection;
        StatusMessage = "New connection added";
    }

    [RelayCommand]
    private void AddToBuilding(string? building)
    {
        if (string.IsNullOrEmpty(building)) return;

        var newConnection = new PlcConnection
        {
            Name = "New PLC",
            NetworkAdapter = AvailableAdapters.FirstOrDefault() ?? string.Empty,
            Status = "Not connected",
            Building = building
        };
        Connections.Add(newConnection);
        SelectedConnection = newConnection;
        StatusMessage = $"Added new PLC to {building}";
        OnPropertyChanged(nameof(GroupedConnections));
    }

    [RelayCommand]
    private void Delete(PlcConnection? connection)
    {
        if (connection != null)
        {
            var name = connection.Name;
            Connections.Remove(connection);
            StatusMessage = $"Deleted '{name}'";
            _ = SaveConnectionsAsync();
        }
    }

    [RelayCommand]
    private async Task ConnectAsync(PlcConnection? connection)
    {
        if (connection == null)
        {
            StatusMessage = "No connection selected";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Connecting to {connection.Name}...";

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(connection.MyIpAddress))
            {
                StatusMessage = "Error: My IP Address is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(connection.NetworkAdapter))
            {
                StatusMessage = "Error: Network Adapter is required";
                return;
            }

            // Change IP address
            var (success, message) = await _networkService.ChangeIpAddressAsync(
                connection.NetworkAdapter,
                connection.MyIpAddress,
                connection.SubnetMask,
                connection.Gateway ?? string.Empty);

            if (success)
            {
                connection.Status = "Connected";
                connection.LastConnected = DateTime.Now;
                StatusMessage = $"Connected to {connection.Name} - IP changed successfully";
                await SaveConnectionsAsync();
            }
            else
            {
                connection.Status = "Failed";
                StatusMessage = message;
            }
        }
        catch (Exception ex)
        {
            connection.Status = "Error";
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PingAsync(PlcConnection? connection)
    {
        if (connection == null)
        {
            StatusMessage = "No connection selected";
            return;
        }

        if (string.IsNullOrWhiteSpace(connection.PlcIpAddress))
        {
            StatusMessage = "PLC IP Address is required for ping";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Pinging {connection.PlcIpAddress}...";

        try
        {
            var success = await _networkService.PingAsync(connection.PlcIpAddress);

            if (success)
            {
                connection.Status = "Online";
                StatusMessage = $"Ping successful: {connection.PlcIpAddress} is online";
            }
            else
            {
                connection.Status = "Offline";
                StatusMessage = $"Ping failed: {connection.PlcIpAddress} is not responding";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ping error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveConnectionsAsync();
    }

    [RelayCommand]
    private Task SaveAsAsync()
    {
        // This will be called from the view with a file path
        StatusMessage = "Use File > Save As... from the menu";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task LoadAsync()
    {
        // This will be called from the view with a file path
        StatusMessage = "Use File > Open... from the menu";
        return Task.CompletedTask;
    }

    public async Task SaveConnectionsAsync(string? filePath = null)
    {
        try
        {
            IsLoading = true;
            await _dataService.SaveConnectionsAsync(Connections, filePath);
            StatusMessage = filePath != null ? $"Saved to {filePath}" : "Saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadConnectionsAsync(string? filePath = null)
    {
        try
        {
            IsLoading = true;
            var connections = await _dataService.LoadConnectionsAsync(filePath);
            Connections.Clear();
            foreach (var connection in connections)
            {
                Connections.Add(connection);
            }
            StatusMessage = filePath != null ? $"Loaded from {filePath}" : "Loaded successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
