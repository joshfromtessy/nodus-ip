using Avalonia.Controls;
using PLCManager.ViewModels;

namespace PLCManager.Views;

public partial class TestWindow : Window
{
    public TestWindow()
    {
        InitializeComponent();

        // Set DataContext in code-behind to ensure it's set
        var vm = new MainWindowViewModel();
        DataContext = vm;

        // Debug: Check if data exists
        System.Diagnostics.Debug.WriteLine($"Connections count in constructor: {vm.Connections.Count}");
    }
}
