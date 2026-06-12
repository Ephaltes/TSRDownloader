namespace TSRDownloader.Core.Models;

/// <summary>
/// Represents a single TSR download item with its current state and progress.
/// </summary>
public class DownloadItem
{
    /// <summary>Unique identifier from the TSR URL.</summary>
    public int ItemId { get; set; }

    /// <summary>The full TSR URL that was copied from the clipboard.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>The file name once known (set during download).</summary>
    public string? FileName { get; set; }

    /// <summary>Full path of the downloaded file on disk (set once the download completes).</summary>
    public string? FilePath { get; set; }

    /// <summary>Current download status.</summary>
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;

    /// <summary>Download progress as a percentage (0–100).</summary>
    public double Progress { get; set; }

    /// <summary>Total bytes to download (set once headers are received).</summary>
    public long TotalBytes { get; set; }

    /// <summary>Bytes downloaded so far.</summary>
    public long DownloadedBytes { get; set; }

    /// <summary>Error message if the download failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Timestamp when the item was added to the queue.</summary>
    public DateTime AddedAt { get; set; } = DateTime.Now;

    /// <summary>Timestamp when the download completed or failed.</summary>
    public DateTime? CompletedAt { get; set; }
}
