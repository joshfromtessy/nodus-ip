using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PLCManager.Models;
using PLCManager.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PLCManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NetworkService _networkService;
    private readonly DataService _dataService;
    private readonly UpdateService _updateService;

    public ObservableCollection<PlcConnection> Connections { get; } = new ObservableCollection<PlcConnection>();

    [ObservableProperty]
    private PlcConnection? _selectedConnection;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string _latestVersion = string.Empty;

    [ObservableProperty]
    private string _updateUrl = string.Empty;

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
        _updateService = new UpdateService();

        LoadAvailableAdapters();

        // Auto-load from default location
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var connections = await _dataService.LoadConnectionsAsync();
            foreach (var connection in connections)
            {
                Connections.Add(connection);
            }
            OnPropertyChanged(nameof(GroupedConnections));
            StatusMessage = connections.Count > 0
                ? $"Loaded {connections.Count} connection(s)"
                : "Ready - Click + to add new connections";

            // Check for updates after loading
            await CheckForUpdatesAsync();
        }
        catch
        {
            StatusMessage = "Ready - Click + to add new connections";
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var (updateAvailable, latestVersion, downloadUrl) = await _updateService.CheckForUpdatesAsync();

            if (updateAvailable)
            {
                UpdateAvailable = true;
                LatestVersion = latestVersion;
                UpdateUrl = downloadUrl;
                StatusMessage = $"Update available: v{latestVersion} - Click 🔄 to download";
            }
        }
        catch
        {
            // Silently fail - don't bother user if update check fails
        }
    }

    [RelayCommand]
    private void OpenUpdateUrl()
    {
        if (!string.IsNullOrEmpty(UpdateUrl))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = UpdateUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                StatusMessage = "Failed to open browser";
            }
        }
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
    private async Task Delete(PlcConnection? connection)
    {
        if (connection != null)
        {
            var name = connection.Name;
            Connections.Remove(connection);
            OnPropertyChanged(nameof(GroupedConnections));

            // Auto-save after delete
            await AutoSaveAsync();
            StatusMessage = $"Deleted '{name}'";
        }
    }

    private async Task AutoSaveAsync()
    {
        try
        {
            await _dataService.SaveConnectionsAsync(Connections);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Auto-save failed: {ex.Message}";
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
        StatusMessage = $"Setting local IP for {connection.Name}...";

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
                StatusMessage = $"Local IP set successfully for {connection.Name}";
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
            if (filePath != null)
            {
                StatusMessage = $"Exported to {Path.GetFileName(filePath)}";
            }
            // Don't show message for auto-saves (when filePath is null)
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

            if (filePath != null)
            {
                // Manual load - auto-save the imported connections
                await AutoSaveAsync();
                StatusMessage = $"Imported {connections.Count} connection(s) from {Path.GetFileName(filePath)}";
            }
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
