namespace TSRDownloader.Core.Models;

/// <summary>
/// Application configuration. Defaults come from appsettings.json; the user's values are
/// persisted to settings.json in the data directory.
/// </summary>
public class AppConfig
{
    /// <summary>Allowed range for <see cref="MaxActiveDownloads"/>.</summary>
    public const int MinActiveDownloads = 1;
    public const int MaxActiveDownloadsLimit = 10;

    /// <summary>Allowed range (seconds) for <see cref="TicketActivationDelaySeconds"/>.</summary>
    public const int MinTicketActivationDelaySeconds = 0;
    public const int MaxTicketActivationDelaySeconds = 60;

    /// <summary>Download directory used when none (or an empty one) is configured.</summary>
    public static string DefaultDownloadDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "TSR");

    /// <summary>Directory where downloaded files are saved.</summary>
    public string DownloadDirectory { get; set; } = DefaultDownloadDirectory;

    /// <summary>Maximum number of concurrent downloads.</summary>
    public int MaxActiveDownloads { get; set; } = 3;

    /// <summary>UI language as a two-letter code ("de" or "en").</summary>
    public string Language { get; set; } = "en";

    /// <summary>Whether to show a Windows notification when a new link is detected and queued.</summary>
    public bool NotifyOnLinkFound { get; set; } = true;

    /// <summary>Whether the application should launch automatically when the user logs in.</summary>
    public bool StartOnSystemStartup { get; set; }

    /// <summary>Whether downloaded .zip archives are extracted automatically.</summary>
    public bool ExtractZipArchives { get; set; } = true;

    /// <summary>Whether a .zip is deleted after it was extracted successfully.</summary>
    public bool DeleteZipAfterExtraction { get; set; } = true;

    /// <summary>
    /// Seconds TSR needs between requesting a download ticket and resolving the download URL.
    /// Increase if downloads fail with an invalid ticket.
    /// </summary>
    public int TicketActivationDelaySeconds { get; set; } = 8;

    /// <summary>How new application releases are handled at startup.</summary>
    public UpdateMode UpdateMode { get; set; } = UpdateMode.Automatic;
}
