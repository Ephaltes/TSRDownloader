using Velopack.Locators;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Locations of the user's data (settings, download queue, logs). Velopack replaces the whole
/// <c>current\</c> install folder on every update, so an installed app keeps its data one
/// level up in the install root (<c>%LocalAppData%\TSRDownloader</c>), which survives
/// updates and is removed on uninstall. A development build keeps it next to the binaries.
/// </summary>
public sealed class AppPaths
{
    /// <summary>Process environment variable pointing at <see cref="DataDirectory"/>; used by the log sink path in appsettings.json.</summary>
    public const string DataDirectoryVariable = "TSRDOWNLOADER_DATA";

    /// <summary>Directory that holds all user data.</summary>
    public string DataDirectory { get; }

    /// <summary>The user's settings file.</summary>
    public string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    /// <summary>The saved download list, restored on the next start.</summary>
    public string QueueFile => Path.Combine(DataDirectory, "queue.json");

    /// <summary>Directory for rolling log files.</summary>
    public string LogDirectory => Path.Combine(DataDirectory, "logs");

    public AppPaths(string? installedRootDirectory, string fallbackDirectory)
    {
        DataDirectory = string.IsNullOrEmpty(installedRootDirectory)
            ? fallbackDirectory
            : installedRootDirectory;
    }

    /// <summary>
    /// Resolves the paths from the Velopack locator. Must be called after
    /// <c>VelopackApp.Build().Run()</c>.
    /// </summary>
    public static AppPaths FromVelopack()
    {
        string? installedRoot = null;
        if (VelopackLocator.IsCurrentSet && VelopackLocator.Current.CurrentlyInstalledVersion is not null)
            installedRoot = VelopackLocator.Current.RootAppDir;

        return new AppPaths(installedRoot, AppContext.BaseDirectory);
    }
}
