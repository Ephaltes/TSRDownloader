using System.Diagnostics;
using System.Windows.Navigation;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI.Views;

/// <summary>
/// Shows the app's name, version and purpose, with links to the project page and its issue tracker.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow(string versionText)
    {
        InitializeComponent();
        VersionText.Text = versionText;
        ProjectPageLink.NavigateUri = new Uri(VelopackUpdateService.RepositoryUrl);
        ReportProblemLink.NavigateUri = new Uri($"{VelopackUpdateService.RepositoryUrl}/issues");
    }

    private void OnLinkRequestNavigate(object sender, RequestNavigateEventArgs eventArgs)
    {
        try
        {
            Process.Start(new ProcessStartInfo(eventArgs.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // No browser registered — nothing sensible to do.
        }
        eventArgs.Handled = true;
    }
}
