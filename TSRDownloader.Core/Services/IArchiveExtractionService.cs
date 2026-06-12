using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Serially extracts downloaded archives (one at a time) on a background worker.
/// Downloads may run ahead; extraction order is FIFO and never concurrent.
/// </summary>
public interface IArchiveExtractionService
{
    /// <summary>Raised when an enqueued item's extraction state or progress changes.</summary>
    event Action<DownloadItem>? ItemChanged;

    /// <summary>Raised when an item reaches a terminal extraction state (for stats refresh).</summary>
    event Action? StatsChanged;

    /// <summary>Adds a downloaded archive item to the extraction queue.</summary>
    void Enqueue(DownloadItem item);
}
