using CommunityToolkit.WinUI.Notifications;
using TSRDownloader.UI.Services;
using Velopack;

namespace TSRDownloader.UI;

/// <summary>
/// Custom entry point: Velopack must run before anything else, because the installer and
/// updater launch the exe with hook arguments and expect it to exit from inside <c>Run()</c>.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ =>
            {
                // Fast callback: no UI, must return quickly. Best-effort cleanup only.
                try { StartupService.RemoveRegistration(); } catch { /* nothing to report to */ }
                try { ToastNotificationManagerCompat.Uninstall(); } catch { /* nothing to report to */ }
            })
            .Run();

        App app = new();
        app.InitializeComponent();
        app.Run();
    }
}
