namespace TSRDownloader.Core.Services;

/// <summary>
/// Keeps the saved download list in sync with <see cref="IDownloadService"/>: every status
/// change (add, remove, start, finish, extraction done) schedules a save, coalesced over a
/// short delay so bursts of changes cause a single write. Pending changes are written on
/// <see cref="Flush"/> and on <see cref="Dispose"/> (application shutdown).
/// </summary>
public sealed class DownloadQueueAutosave : IDisposable
{
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(1);

    private readonly IDownloadService _downloadService;
    private readonly IArchiveExtractionService _extractionService;
    private readonly IDownloadQueueStore _store;
    private readonly TimeSpan _delay;
    private readonly Timer _timer;
    private readonly Lock _lock = new();

    // Only write after a real change, so an app that exits before restoring (e.g. to install
    // an update) never overwrites the saved queue with an empty one.
    private bool _isDirty;
    private bool _isDisposed;

    public DownloadQueueAutosave(
        IDownloadService downloadService,
        IArchiveExtractionService extractionService,
        IDownloadQueueStore store,
        TimeSpan? delay = null)
    {
        _downloadService = downloadService;
        _extractionService = extractionService;
        _store = store;
        _delay = delay ?? DefaultDelay;
        _timer = new Timer(_ => Flush());

        _downloadService.QueueStatsChanged += OnQueueChanged;
        _extractionService.StatsChanged += OnQueueChanged;
    }

    private void OnQueueChanged()
    {
        lock (_lock)
        {
            if (_isDisposed)
                return;
            _isDirty = true;
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Writes the current list now if it changed since the last write.</summary>
    public void Flush()
    {
        lock (_lock)
        {
            if (!_isDirty)
                return;
            _isDirty = false;
            _store.Save(_downloadService.Items);
        }
    }

    public void Dispose()
    {
        _downloadService.QueueStatsChanged -= OnQueueChanged;
        _extractionService.StatsChanged -= OnQueueChanged;

        lock (_lock)
            _isDisposed = true;

        _timer.Dispose();
        Flush();
    }
}
