using Avalonia.Controls;
using Avalonia.Interactivity;
using PLCManager.Models;
using System.Linq;

namespace PLCManager.Views;

public partial class PlcEditDialog : Window
{
    public PlcConnection? Result { get; private set; }
    private readonly string[] _availableAdapters;

    public PlcEditDialog(string[] availableAdapters, PlcConnection? existingConnection = null)
    {
        InitializeComponent();
        _availableAdapters = availableAdapters;

        // Populate adapters
        foreach (var adapter in availableAdapters)
        {
            AdapterComboBox.Items.Add(adapter);
        }

        // If editing existing connection, populate fields
        if (existingConnection != null)
        {
            NameTextBox.Text = existingConnection.Name;
            PlcIpTextBox.Text = existingConnection.PlcIpAddress;
            MyIpTextBox.Text = existingConnection.MyIpAddress;
            SubnetMaskTextBox.Text = existingConnection.SubnetMask;
            GatewayTextBox.Text = existingConnection.Gateway;
            NotesTextBox.Text = existingConnection.Notes;

            // Select building
            var buildingItem = BuildingComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content?.ToString() == existingConnection.Building);
            if (buildingItem != null)
                BuildingComboBox.SelectedItem = buildingItem;

            // Select adapter
            AdapterComboBox.SelectedItem = existingConnection.NetworkAdapter;

            Title = "Edit PLC Connection";
        }
        else
        {
            // Default values for new connection
            BuildingComboBox.SelectedIndex = 0; // West
            if (availableAdapters.Length > 0)
                AdapterComboBox.SelectedIndex = 0;
        }
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
