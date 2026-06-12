using System.Collections.Concurrent;
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
    private const long ProgressNotifyIntervalMs = 100;

    private readonly ITsrDownloadClient _tsrClient;
    private readonly IConfigService _configService;
    private readonly IArchiveExtractionService _extractionService;
    private readonly List<DownloadItem> _items = [];
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _activeTokens = new();
    private readonly Lock _lock = new();

    private volatile int _maxActiveDownloads;
    private volatile bool _isRunning;

    public event Action<DownloadItem>? ItemChanged;
    public event Action? QueueStatsChanged;

    public IReadOnlyList<DownloadItem> Items
    {
        get { lock (_lock) return _items.ToList().AsReadOnly(); }
    }

    public DownloadService(
        ITsrDownloadClient tsrClient,
        IConfigService configService,
        IArchiveExtractionService extractionService)
    {
        _tsrClient = tsrClient;
        _configService = configService;
        _extractionService = extractionService;
        _maxActiveDownloads = Math.Max(1, configService.Config.MaxActiveDownloads);
    }

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
        NotifyStatsChanged();
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
        NotifyStatsChanged();
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
                if (!_configService.Config.ExtractZipArchives)
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
            if (!_items.Contains(item) || item.Status is not (DownloadStatus.Failed or DownloadStatus.ExtractionFailed))
                return false;

            Requeue(item);
        }

        NotifyItemChanged(item);
        NotifyStatsChanged();
        StartQueuedDownloads();
        return true;
    }

    public bool Remove(DownloadItem item)
    {
        lock (_lock)
        {
            // Items handed to the extraction queue are owned by it until extraction finishes.
            if (item.Status is DownloadStatus.WaitingToExtract or DownloadStatus.Extracting || !_items.Remove(item))
                return false;
        }

        if (_activeTokens.TryGetValue(item.ItemId, out CancellationTokenSource? cts))
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { /* finished meanwhile */ }
        }

        NotifyStatsChanged();
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

        foreach (CancellationTokenSource cts in _activeTokens.Values)
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { /* finished meanwhile */ }
        }
        NotifyStatsChanged();
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

            int activeCount = _items.Count(i => i.Status == DownloadStatus.Downloading);
            foreach (DownloadItem item in _items)
            {
                if (activeCount >= _maxActiveDownloads)
                    break;
                if (item.Status != DownloadStatus.Queued)
                    continue;

                item.Status = DownloadStatus.Downloading;
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
        NotifyStatsChanged();

        CancellationTokenSource cts = new();
        _activeTokens[item.ItemId] = cts;

        try
        {
            await DownloadItemAsync(item, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Stopped (or removed): back to the queue; a partial .part file is resumed later.
            item.Status = DownloadStatus.Queued;
        }
        catch (Exception ex)
        {
            // Includes HTTP timeouts, which surface as OperationCanceledException without our token
            // being cancelled — re-queueing those would retry forever.
            item.Status = DownloadStatus.Failed;
            item.ErrorMessage = ex.Message;
        }
        finally
        {
            // Only remove our own entry; a restart may already have registered a new one.
            _activeTokens.TryRemove(new KeyValuePair<int, CancellationTokenSource>(item.ItemId, cts));
            cts.Dispose();
            NotifyItemChanged(item);
            NotifyStatsChanged();
            StartQueuedDownloads();
        }
    }

    private async Task DownloadItemAsync(DownloadItem item, CancellationToken ct)
    {
        TsrDownloadInfo info = await _tsrClient.ResolveDownloadAsync(item.ItemId, ct);

        item.FileName = info.FileName;
        NotifyItemChanged(item);

        // Progress arrives per network chunk; throttle so the UI is not flooded.
        long lastNotifyMs = 0;
        string filePath = await _tsrClient.DownloadFileAsync(
            info,
            _configService.Config.DownloadDirectory,
            progress =>
            {
                item.DownloadedBytes = progress.DownloadedBytes;
                item.TotalBytes = progress.TotalBytes;
                item.Progress = progress.Percentage;

                long nowMs = Environment.TickCount64;
                if (nowMs - lastNotifyMs < ProgressNotifyIntervalMs)
                    return;
                lastNotifyMs = nowMs;
                NotifyItemChanged(item);
            },
            ct);

        item.FilePath = filePath;
        item.CompletedAt = DateTime.Now;
        item.Progress = 100;

        if (_configService.Config.ExtractZipArchives && IsZipFile(filePath))
        {
            item.Status = DownloadStatus.WaitingToExtract;
            _extractionService.Enqueue(item);
        }
        else
        {
            item.Status = DownloadStatus.Completed;
        }
    }

    private static bool IsZipFile(string path) =>
        Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    private void NotifyItemChanged(DownloadItem item) => ItemChanged?.Invoke(item);

    private void NotifyStatsChanged() => QueueStatsChanged?.Invoke();

    public void Dispose() => Stop();
}
