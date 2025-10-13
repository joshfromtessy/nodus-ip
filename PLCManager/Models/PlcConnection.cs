using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace PLCManager.Models;

public partial class PlcConnection : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _plcIpAddress = string.Empty;

    [ObservableProperty]
    private string _myIpAddress = string.Empty;

    [ObservableProperty]
    private string _subnetMask = "255.255.255.0";

    [ObservableProperty]
    private string _gateway = string.Empty;

    [ObservableProperty]
    private string _networkAdapter = string.Empty;

    [ObservableProperty]
    private string _status = "Unknown";

    [ObservableProperty]
    private DateTime _lastConnected = DateTime.MinValue;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _building = "West";
}
