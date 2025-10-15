using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PLCManager.Models;
using PLCManager.Services;
using System.Linq;

namespace PLCManager.Views;

public partial class PlcEditDialog : Window
{
    public PlcConnection? Result { get; private set; }
    private readonly NetworkService _networkService;
    private PlcConnection? _existingConnection;

    public PlcEditDialog(string[] availableAdapters, PlcConnection? existingConnection = null)
    {
        InitializeComponent();
        _networkService = new NetworkService();
        _existingConnection = existingConnection;

        // Populate adapters
        PopulateAdapters(availableAdapters);

        PopulateFields();
    }

    private void PopulateAdapters(string[] adapters)
    {
        var selectedAdapter = AdapterComboBox.SelectedItem?.ToString();

        AdapterComboBox.Items.Clear();
        foreach (var adapter in adapters)
        {
            AdapterComboBox.Items.Add(adapter);
        }

        // Try to restore selection
        if (!string.IsNullOrEmpty(selectedAdapter))
        {
            AdapterComboBox.SelectedItem = selectedAdapter;
        }
        else if (_existingConnection != null)
        {
            AdapterComboBox.SelectedItem = _existingConnection.NetworkAdapter;
        }
        else if (adapters.Length > 0)
        {
            AdapterComboBox.SelectedIndex = 0;
        }
    }

    private void PopulateFields()
    {
        // If editing existing connection, populate fields
        if (_existingConnection != null)
        {
            NameTextBox.Text = _existingConnection.Name;
            PlcIpTextBox.Text = _existingConnection.PlcIpAddress;
            MyIpTextBox.Text = _existingConnection.MyIpAddress;
            SubnetMaskTextBox.Text = _existingConnection.SubnetMask;
            GatewayTextBox.Text = _existingConnection.Gateway;
            NotesTextBox.Text = _existingConnection.Notes;

            // Select building
            var buildingItem = BuildingComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content?.ToString() == _existingConnection.Building);
            if (buildingItem != null)
                BuildingComboBox.SelectedItem = buildingItem;

            TitleTextBlock.Text = "Edit Connection";
        }
        else
        {
            // Default values for new connection
            BuildingComboBox.SelectedIndex = 0; // West
            TitleTextBlock.Text = "Add Connection";
        }
    }

    private void OnRefreshAdapters(object? sender, RoutedEventArgs e)
    {
        var showInactive = ShowInactiveCheckBox.IsChecked ?? false;
        var adapters = _networkService.GetNetworkAdapters(showInactive);
        PopulateAdapters(adapters);
    }

    private void OnShowInactiveChanged(object? sender, RoutedEventArgs e)
    {
        OnRefreshAdapters(sender, e);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private bool IsValidIpAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;

        var parts = ip.Split('.');
        if (parts.Length != 4) return false;

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out int value))
                return false;
            if (value < 0 || value > 255)
                return false;
        }
        return true;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ErrorTextBlock.Text = "Connection Name is required";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(PlcIpTextBox.Text))
        {
            ErrorTextBlock.Text = "PLC IP Address is required";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (!IsValidIpAddress(PlcIpTextBox.Text))
        {
            ErrorTextBlock.Text = "PLC IP Address is invalid (e.g., 192.168.1.100)";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(MyIpTextBox.Text))
        {
            ErrorTextBlock.Text = "My IP Address is required";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (!IsValidIpAddress(MyIpTextBox.Text))
        {
            ErrorTextBlock.Text = "My IP Address is invalid (e.g., 192.168.1.10)";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(SubnetMaskTextBox.Text))
        {
            ErrorTextBlock.Text = "Subnet Mask is required";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (!IsValidIpAddress(SubnetMaskTextBox.Text))
        {
            ErrorTextBlock.Text = "Subnet Mask is invalid (e.g., 255.255.255.0)";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(GatewayTextBox.Text) && !IsValidIpAddress(GatewayTextBox.Text))
        {
            ErrorTextBlock.Text = "Gateway is invalid (e.g., 192.168.1.1)";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (BuildingComboBox.SelectedItem == null)
        {
            ErrorTextBlock.Text = "Please select a Building";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (AdapterComboBox.SelectedItem == null)
        {
            ErrorTextBlock.Text = "Please select a Network Adapter";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        Result = new PlcConnection
        {
            Name = NameTextBox.Text,
            PlcIpAddress = PlcIpTextBox.Text,
            MyIpAddress = MyIpTextBox.Text,
            SubnetMask = SubnetMaskTextBox.Text,
            Gateway = GatewayTextBox.Text ?? string.Empty,
            NetworkAdapter = AdapterComboBox.SelectedItem?.ToString() ?? string.Empty,
            Building = (BuildingComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "West",
            Notes = NotesTextBox.Text ?? string.Empty,
            Status = "Not connected"
        };

        Close(Result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
