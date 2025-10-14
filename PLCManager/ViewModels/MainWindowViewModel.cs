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

    public ObservableCollection<PlcConnection> Connections { get; } = new ObservableCollection<PlcConnection>();

    [ObservableProperty]
    private PlcConnection? _selectedConnection;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<string> _availableAdapters = new ObservableCollection<string>();

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<string> Buildings { get; } = new ObservableCollection<string>
    {
        "West",
        "East",
        "South",
        "North",
        "Skaneateles",
        "Auburn"
    };

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(GroupedConnections));
    }

    public ObservableCollection<BuildingGroup> GroupedConnections
    {
        get
        {
            var groups = new ObservableCollection<BuildingGroup>();
            foreach (var building in Buildings)
            {
                var connectionsInBuilding = Connections
                    .Where(c => c.Building == building)
                    .Where(c => string.IsNullOrWhiteSpace(SearchText) ||
                               c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

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

        // Don't auto-load - user can use 📂 button to load
        StatusMessage = "Ready - Use 📂 to load connections or + to add new ones";
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
        // This will be handled by the view - it will open the dialog
        StatusMessage = "Add new PLC connection...";
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
            OnPropertyChanged(nameof(GroupedConnections));
            StatusMessage = $"Deleted '{name}' - Click 💾 to save changes";
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
            OnPropertyChanged(nameof(GroupedConnections));
            StatusMessage = filePath != null ? $"Loaded from {filePath}" : "Ready";
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
