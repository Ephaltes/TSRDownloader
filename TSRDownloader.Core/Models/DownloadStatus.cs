namespace TSRDownloader.Core.Models;

/// <summary>
/// Represents the current state of a download item in the queue.
/// </summary>
public enum DownloadStatus
{
    /// <summary>Item is queued and waiting to be downloaded.</summary>
    Queued,
    /// <summary>Item is currently being downloaded.</summary>
    Downloading,
    /// <summary>Download completed successfully.</summary>
    Completed,
    /// <summary>Download failed due to an error.</summary>
    Failed,
    /// <summary>Download finished; archive is waiting in the extraction queue.</summary>
    WaitingToExtract,
    /// <summary>Archive is currently being extracted.</summary>
    Extracting,
    /// <summary>Archive was extracted successfully.</summary>
    ExtractionCompleted,
    /// <summary>Archive extraction failed.</summary>
    ExtractionFailed
}
