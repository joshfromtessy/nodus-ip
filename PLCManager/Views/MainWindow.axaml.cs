using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PLCManager.Models;
using PLCManager.ViewModels;
using System.Linq;

namespace PLCManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        // Subscribe to selection changes to update UI
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedConnection))
            {
                UpdateSelectionHighlight();
            }
        };
    }

    private void UpdateSelectionHighlight()
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null) return;

        // Force visual update
        var temp = DataContext;
        DataContext = null;
        DataContext = temp;
    }

    private void OnItemLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is PlcConnection connection)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel != null && viewModel.SelectedConnection == connection)
            {
                if (button.Parent is Border border)
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(0x58, 0x65, 0xF2)); // #5865F2
                }
            }
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnAddNewClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null) return;

        var dialog = new PlcEditDialog(viewModel.AvailableAdapters.ToArray());
        var result = await dialog.ShowDialog<PlcConnection?>(this);

        if (result != null)
        {
            viewModel.Connections.Add(result);
            viewModel.SelectedConnection = result;

            // Force UI refresh by clearing and re-setting DataContext
            var temp = DataContext;
            DataContext = null;
            DataContext = temp;

            viewModel.StatusMessage = $"Added '{result.Name}' to {result.Building}";
        }
    }

    private async void OnConnectionDoubleClick(object? sender, TappedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null || viewModel.SelectedConnection == null) return;

        var dialog = new PlcEditDialog(
            viewModel.AvailableAdapters.ToArray(),
            viewModel.SelectedConnection);

        var result = await dialog.ShowDialog<PlcConnection?>(this);

        if (result != null)
        {
            // Get the index of the current connection
            var index = viewModel.Connections.IndexOf(viewModel.SelectedConnection);

            if (index >= 0)
            {
                // Remove the old one and insert the updated one
                viewModel.Connections.RemoveAt(index);
                viewModel.Connections.Insert(index, result);
                viewModel.SelectedConnection = result;

                // Force UI refresh by clearing and re-setting DataContext
                var temp = DataContext;
                DataContext = null;
                DataContext = temp;

                viewModel.StatusMessage = "Connection updated";
            }
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PLC Connections",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON Files")
                {
                    Patterns = new[] { "*.json" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Any())
        {
            var file = files[0];
            await viewModel.LoadConnectionsAsync(file.Path.LocalPath);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null) return;

        // Show file picker to choose save location
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PLC Connections",
            DefaultExtension = "json",
            SuggestedFileName = "plc-connections.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON Files")
                {
                    Patterns = new[] { "*.json" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (file != null)
        {
            await viewModel.SaveConnectionsAsync(file.Path.LocalPath);
        }
    }

    private async void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null) return;

        // Show file picker to choose file to load
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PLC Connections",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON Files")
                {
                    Patterns = new[] { "*.json" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Any())
        {
            var file = files[0];
            await viewModel.LoadConnectionsAsync(file.Path.LocalPath);

            // Force UI refresh
            var temp = DataContext;
            DataContext = null;
            DataContext = temp;
        }
    }

    private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PLC Connections",
            DefaultExtension = "json",
            SuggestedFileName = "plc-connections.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON Files")
                {
                    Patterns = new[] { "*.json" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (file != null)
        {
            await viewModel.SaveConnectionsAsync(file.Path.LocalPath);
        }
    }
}
