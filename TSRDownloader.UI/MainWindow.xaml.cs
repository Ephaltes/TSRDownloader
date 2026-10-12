using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Behaviors;
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
        RestoreLayout();

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

    /// <summary>Applies the size, position and column widths saved by <see cref="SaveLayout"/>.</summary>
    private void RestoreLayout()
    {
        if (_configService.Config.MainWindowLayout is not { } layout)
            return;

        if (layout.IsReachableOn(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = layout.Left;
            Top = layout.Top;
            Width = Math.Max(layout.Width, MinWidth);
            Height = Math.Max(layout.Height, MinHeight);
        }
        if (layout.IsMaximized)
            WindowState = WindowState.Maximized;

        GridViewColumnCollection columns = ((GridView)DownloadList.View).Columns;
        if (layout.ColumnWidths.Count != columns.Count || layout.ColumnWidths.Any(width => width <= 0))
            return;
        for (int i = 0; i < columns.Count; i++)
        {
            // Filling columns keep their share of the width: the saved widths become their weights.
            if (GridViewColumnSizing.GetStarWeight(columns[i]) > 0)
                GridViewColumnSizing.SetStarWeight(columns[i], layout.ColumnWidths[i]);
            else
                columns[i].Width = layout.ColumnWidths[i];
        }
    }

    private void SaveLayout()
    {
        Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (bounds.IsEmpty)
            return;

        _configService.Config.MainWindowLayout = new WindowLayout
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            IsMaximized = WindowState == WindowState.Maximized,
            ColumnWidths = ((GridView)DownloadList.View).Columns.Select(column => column.ActualWidth).ToList()
        };
        _configService.Save();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveLayout();

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
