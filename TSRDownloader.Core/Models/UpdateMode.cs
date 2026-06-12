namespace TSRDownloader.Core.Models;

/// <summary>
/// How the application handles new releases at startup.
/// </summary>
public enum UpdateMode
{
    /// <summary>Check for updates, download and apply them automatically.</summary>
    Automatic,
    /// <summary>Check for updates and notify the user, but do not download them.</summary>
    NotifyOnly,
    /// <summary>Never check for updates.</summary>
    Disabled
}
