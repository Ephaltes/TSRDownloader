using System.Threading.Channels;
using TSRDownloader.Core.Helpers;
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
    private readonly IConfigurationService _configurationService;
    private readonly IFileConflictResolver _conflictResolver;

    private readonly Channel<DownloadItem> _queue =
        Channel.CreateUnbounded<DownloadItem>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _cancellation = new();

    public event Action<DownloadItem>? ItemChanged;
    public event Action? StatisticsChanged;

    public ArchiveExtractionService(
        IArchiveExtractor extractor,
        IConfigurationService configurationService,
        IFileConflictResolver conflictResolver)
    {
        _extractor = extractor;
        _configurationService = configurationService;
        _conflictResolver = conflictResolver;
        _ = Task.Run(ProcessQueueAsync);
    }

    public void Enqueue(DownloadItem item) => _queue.Writer.TryWrite(item);

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (DownloadItem item in _queue.Reader.ReadAllAsync(_cancellation.Token))
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

        try
        {
            await _extractor.ExtractAsync(
                archivePath,
                ArchivePaths.ExtractionFolder(archivePath),
                _conflictResolver,
                percent =>
                {
                    item.Progress = percent;
                    NotifyItemChanged(item);
                },
                _cancellation.Token);

            item.Status = DownloadStatus.ExtractionCompleted;
            item.Progress = 100;

            if (_configurationService.Configuration.DeleteZipAfterExtraction)
                TryDeleteArchive(archivePath);
        }
        catch (OperationCanceledException)
        {
            return; // shutting down — leave the item as-is
        }
        catch (Exception exception)
        {
            item.Status = DownloadStatus.ExtractionFailed;
            item.ErrorMessage = exception.Message;
        }
        finally
        {
            NotifyItemChanged(item);
            NotifyStatisticsChanged();
        }
    }

    private static void TryDeleteArchive(string path)
    {
        try { File.Delete(path); }
        catch { /* keep the archive if deletion fails; extraction still counts as success */ }
    }

    private void NotifyItemChanged(DownloadItem item) => ItemChanged?.Invoke(item);
    private void NotifyStatisticsChanged() => StatisticsChanged?.Invoke();

    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _cancellation.Cancel();
        _cancellation.Dispose();
    }
}
