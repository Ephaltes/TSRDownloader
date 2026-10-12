using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI.Presentation;

/// <summary>The version line shown in the status bar and the settings window.</summary>
public static class AppVersionText
{
    /// <summary>"Version 1.1.7", or "Development build" when not installed via the installer.</summary>
    public static string Format(IUpdateService updateService, LocalizationManager localization) =>
        updateService.IsInstalled
            ? localization.Format("Settings_Version", updateService.CurrentVersion ?? string.Empty)
            : localization.Get("Settings_DevelopmentBuild");
}
