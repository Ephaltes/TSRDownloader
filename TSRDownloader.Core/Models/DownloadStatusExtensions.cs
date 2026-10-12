namespace TSRDownloader.Core.Models;

/// <summary>Groups of <see cref="DownloadStatus"/> values that callers check together.</summary>
public static class DownloadStatusExtensions
{
    /// <summary>Downloaded (and, if it was extracted, extracted) successfully.</summary>
    public static bool IsSucceeded(this DownloadStatus status) =>
        status is DownloadStatus.Completed or DownloadStatus.ExtractionCompleted;

    /// <summary>The download or the extraction failed.</summary>
    public static bool IsFailed(this DownloadStatus status) =>
        status is DownloadStatus.Failed or DownloadStatus.ExtractionFailed;

    /// <summary>Owned by the extraction queue: waiting for or in extraction.</summary>
    public static bool IsInExtraction(this DownloadStatus status) =>
        status is DownloadStatus.WaitingToExtract or DownloadStatus.Extracting;

    /// <summary>No more work will happen for the item: it succeeded or failed.</summary>
    public static bool IsFinished(this DownloadStatus status) => status.IsSucceeded() || status.IsFailed();
}
