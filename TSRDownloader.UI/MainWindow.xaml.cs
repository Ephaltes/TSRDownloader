using TSRDownloader.Core.Services;
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
    private readonly IConfigService _configService;
    private readonly TrayIconService _trayIcon;
    private bool _isExiting;

    public MainWindow(MainViewModel viewModel, ClipboardHookService clipboardHook,
        IConfigService configService, TrayIconService trayIcon)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _clipboardHook = clipboardHook;
        _configService = configService;
        _trayIcon = trayIcon;
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

    /// <summary>Closes the window and quits, even when closing normally only hides it to the tray.</summary>
    public void RequestExit()
    {
        _isExiting = true;
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting && _configService.Config.CloseToTray)
        {
            // Keep watching the clipboard and downloading in the background.
            e.Cancel = true;
            Hide();
            _trayIcon.ShowStillRunningHint();
            return;
        }

        // Unfinished downloads are saved and continue on the next start.
        _viewModel.StopMonitoringCommand.Execute(null);
        _clipboardHook.Dispose();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _viewModel.SetSelection(DownloadList.SelectedItems.Cast<DownloadItemViewModel>());

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.UnicodeText) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>Links dragged in from a browser or text editor are added like pasted ones.</summary>
    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.UnicodeText) is string text)
            _viewModel.AddLinks(text);
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
