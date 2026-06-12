namespace TSRDownloader.UI.Services;

/// <summary>
/// Checks for, downloads and applies application updates from GitHub Releases.
/// </summary>
public interface IUpdateService
{
    /// <summary>Whether the app runs from a Velopack installation (false for development builds).</summary>
    bool IsInstalled { get; }

    /// <summary>The installed version, or null when not installed.</summary>
    string? CurrentVersion { get; }

    /// <summary>Returns the newest release if it is newer than the installed one; otherwise null.</summary>
    Task<AvailableUpdate?> CheckAsync(CancellationToken cancellationToken);

    /// <summary>Downloads the update, reporting progress from 0 to 100.</summary>
    Task DownloadAsync(AvailableUpdate update, Action<int> progress, CancellationToken cancellationToken);

    /// <summary>Exits the app, applies the downloaded update and restarts it.</summary>
    void ApplyAndRestart(AvailableUpdate update);
}
