using System.Threading.Channels;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Single-worker extraction queue. Guarantees archives are extracted one after
/// another in the order they were enqueued. Extracts each archive into a subfolder
/// named after the archive (without the .zip extension), next to the archive.
/// </summary>
public sealed class ArchiveExtractionService : IArchiveExtractionService, IDisposable
{
    private readonly IArchiveExtractor _extractor;
    private readonly IConfigService _configService;
    private readonly IFileConflictResolver _conflictResolver;

    private readonly Channel<DownloadItem> _queue =
        Channel.CreateUnbounded<DownloadItem>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _cts = new();

    public event Action<DownloadItem>? ItemChanged;
    public event Action? StatsChanged;

    public ArchiveExtractionService(
        IArchiveExtractor extractor,
        IConfigService configService,
        IFileConflictResolver conflictResolver)
    {
        _extractor = extractor;
        _configService = configService;
        _conflictResolver = conflictResolver;
        _ = Task.Run(ProcessQueueAsync);
    }

    public void Enqueue(DownloadItem item) => _queue.Writer.TryWrite(item);

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (DownloadItem item in _queue.Reader.ReadAllAsync(_cts.Token))
                await ExtractItemAsync(item);
        }
        catch (OperationCanceledException)
        {
            // Service is shutting down.
        }
    }

    private async Task ExtractItemAsync(DownloadItem item)
    {
        item.Status = DownloadStatus.Extracting;
        item.Progress = 0;
        NotifyItemChanged(item);

        string archivePath = item.FilePath!;
        string destination = Path.Combine(
            Path.GetDirectoryName(archivePath)!,
            Path.GetFileNameWithoutExtension(archivePath));

        try
        {
            await _extractor.ExtractAsync(
                archivePath,
                destination,
                _conflictResolver,
                percent =>
                {
                    item.Progress = percent;
                    NotifyItemChanged(item);
                },
                _cts.Token);

            item.Status = DownloadStatus.ExtractionCompleted;
            item.Progress = 100;

            if (_configService.Config.DeleteZipAfterExtraction)
                TryDeleteArchive(archivePath);
        }
        catch (OperationCanceledException)
        {
            return; // shutting down — leave the item as-is
        }
        catch (Exception ex)
        {
            item.Status = DownloadStatus.ExtractionFailed;
            item.ErrorMessage = ex.Message;
        }
        finally
        {
            NotifyItemChanged(item);
            NotifyStatsChanged();
        }
    }

    private static void TryDeleteArchive(string path)
    {
        try { File.Delete(path); }
        catch { /* keep the archive if deletion fails; extraction still counts as success */ }
    }

    private void NotifyItemChanged(DownloadItem item) => ItemChanged?.Invoke(item);
    private void NotifyStatsChanged() => StatsChanged?.Invoke();

    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _cts.Cancel();
        _cts.Dispose();
    }
}
