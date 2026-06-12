using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;
using TSRDownloader.UI.ViewModels;

namespace TSRDownloader.UI;

/// <summary>
/// Main application window. Initializes the clipboard hook on load.
/// Auto-starts monitoring and checks current clipboard content immediately.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ClipboardHookService _clipboardHook;

    public MainWindow(MainViewModel viewModel, ClipboardHookService clipboardHook)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _clipboardHook = clipboardHook;
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _clipboardHook.Attach(this);

        // Auto-start monitoring
        _viewModel.StartMonitoringCommand.Execute(null);

        // Also check what's currently in the clipboard
        string? currentClipboardText = null;
        try { currentClipboardText = Clipboard.GetText(); }
        catch { /* clipboard might be locked */ }

        if (!string.IsNullOrEmpty(currentClipboardText))
            _viewModel.OnClipboardContentChanged(currentClipboardText);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Unfinished downloads are saved and continue on the next start.
        _viewModel.StopMonitoringCommand.Execute(null);
        _clipboardHook.Dispose();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        LocalizationManager loc = LocalizationManager.Instance;
        MessageBox.Show(this,
            $"{loc.Get("App_Name")}\n\n{loc.Get("About_Message")}",
            loc.Get("Menu_About"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
