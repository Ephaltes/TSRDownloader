using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using TSRDownloader.UI.Localization;
using Forms = System.Windows.Forms;

namespace TSRDownloader.UI.Services;

/// <summary>
/// The notification-area icon: a left click or "Open" shows the window, "Exit" quits. The menu
/// is a WPF <see cref="ContextMenu"/>, so it follows the app's light or dark theme.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly LocalizationManager _localization;
    private readonly INotificationService _notificationService;
    private bool _stillRunningHintShown;

    public event Action? OpenRequested;
    public event Action? ExitRequested;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public TrayIconService(LocalizationManager localization, INotificationService notificationService)
    {
        _localization = localization;
        _notificationService = notificationService;
        _icon = new Forms.NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Text = localization.Get("App_Name"),
            Visible = true
        };
        _icon.MouseUp += OnMouseUp;
    }

    private void OnMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
            OpenRequested?.Invoke();
        else if (e.Button == Forms.MouseButtons.Right)
            ShowMenu();
    }

    private void ShowMenu()
    {
        MenuItem open = new() { Header = _localization.Get("Tray_Open"), FontWeight = FontWeights.SemiBold };
        open.Click += (_, _) => OpenRequested?.Invoke();
        MenuItem exit = new() { Header = _localization.Get("Menu_Exit") };
        exit.Click += (_, _) => ExitRequested?.Invoke();

        ContextMenu menu = new() { Placement = PlacementMode.MousePoint, Items = { open, new Separator(), exit } };
        menu.IsOpen = true;

        // Without the foreground the menu would stay open when the user clicks elsewhere.
        if (PresentationSource.FromVisual(menu) is HwndSource source)
            SetForegroundWindow(source.Handle);
    }

    /// <summary>Tells the user once per session that closing the window did not quit the app.</summary>
    public void ShowStillRunningHint()
    {
        if (_stillRunningHintShown)
            return;
        _stillRunningHintShown = true;
        _notificationService.Notify(_localization.Get("App_Name"), _localization.Get("Tray_StillRunning"));
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
