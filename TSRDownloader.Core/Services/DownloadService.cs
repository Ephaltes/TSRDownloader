using System.Collections.Concurrent;
using TSRDownloader.Core.Helpers;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Manages the download queue with configurable concurrency and progress tracking.
/// The actual TSR download protocol is delegated to <see cref="ITsrDownloadClient"/>;
/// this class is only responsible for queueing, concurrency limiting and surfacing state.
/// </summary>
public sealed class DownloadService : IDownloadService, IDisposable
{
    /// <summary>Minimum time between two progress notifications for the same download.</summary>
    private const long ProgressNotifyIntervalMilliseconds = 100;

    /// <summary>How often a failed download is retried automatically before it is marked failed.</summary>
    public const int MaxAutomaticRetries = 3;

    private readonly ITsrDownloadClient _tsrClient;
    private readonly Func<int, TimeSpan> _retryDelay;
    private readonly IConfigurationService _configurationService;
    private readonly IArchiveExtractionService _extractionService;
    private readonly List<DownloadItem> _items = [];
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _activeDownloadCancellations = new();
    private readonly Lock _lock = new();

    private volatile int _maxActiveDownloads;
    private volatile bool _isRunning;

    public event Action<DownloadItem>? ItemChanged;
    public event Action? QueueStatisticsChanged;

    public IReadOnlyList<DownloadItem> Items
    {
        get { lock (_lock) return _items.ToList().AsReadOnly(); }
    }

    /// <param name="retryDelay">
    /// Wait before automatic retry number 1..<see cref="MaxAutomaticRetries"/>;
    /// defaults to <see cref="DefaultRetryDelay"/>.
    /// </param>
    public DownloadService(
        ITsrDownloadClient tsrClient,
        IConfigurationService configurationService,
        IArchiveExtractionService extractionService,
        Func<int, TimeSpan>? retryDelay = null)
    {
        _tsrClient = tsrClient;
        _configurationService = configurationService;
        _extractionService = extractionService;
        _retryDelay = retryDelay ?? DefaultRetryDelay;
        _maxActiveDownloads = Math.Max(1, configurationService.Configuration.MaxActiveDownloads);
    }

    /// <summary>5, 15, then 45 seconds: gives TSR or the network increasingly more time to recover.</summary>
    public static TimeSpan DefaultRetryDelay(int attempt) => TimeSpan.FromSeconds(5 * Math.Pow(3, attempt - 1));

    public void SetMaxActiveDownloads(int max)
    {
        _maxActiveDownloads = Math.Max(1, max);
        StartQueuedDownloads();
    }

    public void Enqueue(DownloadItem item) => EnqueueRange([item]);

    public void EnqueueRange(IEnumerable<DownloadItem> items)
    {
        DownloadItem[] added = items.ToArray();

        lock (_lock)
            _items.AddRange(added);

        foreach (DownloadItem item in added)
            NotifyItemChanged(item);
        NotifyStatisticsChanged();
        StartQueuedDownloads();
    }

    public void Restore(IEnumerable<DownloadItem> items)
    {
        DownloadItem[] restored = items.ToArray();
        List<DownloadItem> toExtract = [];

        foreach (DownloadItem item in restored)
        {
            if (PrepareRestoredItem(item))
                toExtract.Add(item);
        }

        lock (_lock)
            _items.AddRange(restored);

        foreach (DownloadItem item in toExtract)
            _extractionService.Enqueue(item);

        foreach (DownloadItem item in restored)
            NotifyItemChanged(item);
        NotifyStatisticsChanged();
        StartQueuedDownloads();
    }

    /// <summary>
    /// Turns the state saved at shutdown into a state this run can continue from.
    /// Returns true when the item must go back into the extraction queue.
    /// </summary>
    private bool PrepareRestoredItem(DownloadItem item)
    {
        switch (item.Status)
        {
            case DownloadStatus.Downloading:
                // Interrupted: download again; the client resumes from the .part file.
                Requeue(item);
                return false;

            case DownloadStatus.WaitingToExtract or DownloadStatus.Extracting:
                if (item.FilePath is null || !File.Exists(item.FilePath))
                {
                    // The archive is gone — fetch it again.
                    Requeue(item);
                    return false;
                }
                if (!_configurationService.Configuration.ExtractZipArchives)
                {
                    item.Status = DownloadStatus.Completed;
                    return false;
                }
                item.Status = DownloadStatus.WaitingToExtract;
                item.Progress = 100;
                return true;

            default:
                // Queued keeps waiting; finished and failed items stay as history.
                return false;
        }
    }

    private static void Requeue(DownloadItem item)
    {
        item.Status = DownloadStatus.Queued;
        item.Progress = 0;
        item.ErrorMessage = null;
    }

    public bool Retry(DownloadItem item)
    {
        lock (_lock)
        {
            if (!_items.Contains(item) || !item.Status.IsFailed())
                return false;

            Requeue(item);
        }

        NotifyItemChanged(item);
        NotifyStatisticsChanged();
        StartQueuedDownloads();
        return true;
    }

    public bool Remove(DownloadItem item)
    {
        lock (_lock)
        {
            // Items handed to the extraction queue are owned by it until extraction finishes.
            if (item.Status.IsInExtraction() || !_items.Remove(item))
                return false;
        }

        if (_activeDownloadCancellations.TryGetValue(item.ItemId, out CancellationTokenSource? cancellation))
            TryCancel(cancellation);

        NotifyStatisticsChanged();
        return true;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        StartQueuedDownloads();
    }

    public void Stop()
    {
        _isRunning = false;

        foreach (CancellationTokenSource cancellation in _activeDownloadCancellations.Values)
            TryCancel(cancellation);
        NotifyStatisticsChanged();
    }

    /// <summary>
    /// Claims queued items for every free concurrency slot and starts them. Called whenever
    /// a slot may have opened up (enqueue, retry, start, a download finishing, limit change),
    /// so no background polling is needed.
    /// </summary>
    private void StartQueuedDownloads()
    {
        List<DownloadItem> claimed = [];

        lock (_lock)
        {
            if (!_isRunning)
                return;

            int activeCount = _items.Count(item => item.Status == DownloadStatus.Downloading);
            foreach (DownloadItem item in _items)
            {
                if (activeCount >= _maxActiveDownloads)
                    break;
                if (item.Status != DownloadStatus.Queued)
                    continue;

                item.Status = DownloadStatus.Downloading;
                // No bytes yet: the UI shows "waiting for TSR" until the transfer starts.
                item.DownloadedBytes = 0;
                item.TotalBytes = 0;
                claimed.Add(item);
                activeCount++;
            }
        }

        foreach (DownloadItem item in claimed)
            _ = Task.Run(() => RunDownloadAsync(item));
    }

    private async Task RunDownloadAsync(DownloadItem item)
    {
        NotifyItemChanged(item);
        NotifyStatisticsChanged();

        CancellationTokenSource cancellation = new();
        _activeDownloadCancellations[item.ItemId] = cancellation;

        try
        {
            await DownloadWithRetriesAsync(item, cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Stopped (or removed): back to the queue; a partial .part file is resumed later.
            item.Status = DownloadStatus.Queued;
        }
        catch (Exception exception)
        {
            // Includes HTTP timeouts, which surface as OperationCanceledException without our token
            // being cancelled — re-queueing those would retry forever.
            item.Status = DownloadStatus.Failed;
            item.ErrorMessage = exception.Message;
        }
        finally
        {
            // Only remove our own entry; a restart may already have registered a new one.
            _activeDownloadCancellations.TryRemove(new KeyValuePair<int, CancellationTokenSource>(item.ItemId, cancellation));
            cancellation.Dispose();
            item.RetryAttempt = 0;
            NotifyItemChanged(item);
            NotifyStatisticsChanged();
            StartQueuedDownloads();
        }
    }

    /// <summary>
    /// Downloads the item, retrying failures up to <see cref="MaxAutomaticRetries"/> times with
    /// increasing waits. The item keeps its download slot while waiting; stopping cancels the wait.
    /// </summary>
    private async Task DownloadWithRetriesAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await DownloadItemAsync(item, cancellationToken);
                return;
            }
            catch (Exception) when (item.RetryAttempt < MaxAutomaticRetries && !cancellationToken.IsCancellationRequested)
            {
                item.RetryAttempt++;
                item.DownloadedBytes = 0;
                item.TotalBytes = 0;
                NotifyItemChanged(item);
                await Task.Delay(_retryDelay(item.RetryAttempt), cancellationToken);
            }
        }
    }

    private async Task DownloadItemAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        TsrDownloadInfo downloadInfo = await _tsrClient.ResolveDownloadAsync(item.ItemId, cancellationToken);

        item.FileName = downloadInfo.FileName;
        NotifyItemChanged(item);

        // Progress arrives per network chunk; throttle so the UI is not flooded.
        long lastNotifyMilliseconds = 0;
        string filePath = await _tsrClient.DownloadFileAsync(
            downloadInfo,
            _configurationService.Configuration.DownloadDirectory,
            progress =>
            {
                item.DownloadedBytes = progress.DownloadedBytes;
                item.TotalBytes = progress.TotalBytes;
                item.Progress = progress.Percentage;

                long nowMilliseconds = Environment.TickCount64;
                if (nowMilliseconds - lastNotifyMilliseconds < ProgressNotifyIntervalMilliseconds)
                    return;
                lastNotifyMilliseconds = nowMilliseconds;
                NotifyItemChanged(item);
            },
            cancellationToken);

        item.FilePath = filePath;
        item.CompletedAt = DateTime.Now;
        item.Progress = 100;

        if (_configurationService.Configuration.ExtractZipArchives && ArchivePaths.IsZip(filePath))
        {
            item.Status = DownloadStatus.WaitingToExtract;
            _extractionService.Enqueue(item);
        }
        else
        {
            item.Status = DownloadStatus.Completed;
        }
    }

    private static void TryCancel(CancellationTokenSource cancellation)
    {
        try { cancellation.Cancel(); }
        catch (ObjectDisposedException) { /* the download finished meanwhile */ }
    }

    private void NotifyItemChanged(DownloadItem item) => ItemChanged?.Invoke(item);

    private void NotifyStatisticsChanged() => QueueStatisticsChanged?.Invoke();

    public void Dispose() => Stop();
}
