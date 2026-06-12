using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Service for managing the TSR download queue and executing downloads.
/// </summary>
public interface IDownloadService
{
    /// <summary>Event raised when a download item's state changes.</summary>
    event Action<DownloadItem>? ItemChanged;

    /// <summary>Event raised when overall queue stats change.</summary>
    event Action? QueueStatsChanged;

    /// <summary>All download items currently tracked.</summary>
    IReadOnlyList<DownloadItem> Items { get; }

    /// <summary>Enqueues a new download item.</summary>
    void Enqueue(DownloadItem item);

    /// <summary>Enqueues multiple download items.</summary>
    void EnqueueRange(IEnumerable<DownloadItem> items);

    /// <summary>
    /// Adds items saved by a previous run. Interrupted downloads are queued again (and resume
    /// from their partial file); archives that were waiting for or in extraction go back into
    /// the extraction queue; finished and failed items are kept as they were.
    /// </summary>
    void Restore(IEnumerable<DownloadItem> items);

    /// <summary>
    /// Re-queues a failed item (download or extraction failure).
    /// Returns false if the item is unknown or has not failed.
    /// </summary>
    bool Retry(DownloadItem item);

    /// <summary>
    /// Removes an item from the list, cancelling it if it is downloading. Items waiting for or
    /// in extraction cannot be removed. Returns whether the item was removed.
    /// </summary>
    bool Remove(DownloadItem item);

    /// <summary>Starts processing the download queue.</summary>
    void Start();

    /// <summary>Stops processing and cancels active downloads.</summary>
    void Stop();

    /// <summary>Updates the max active downloads setting at runtime.</summary>
    void SetMaxActiveDownloads(int max);
}
