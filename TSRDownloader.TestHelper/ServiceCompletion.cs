using System;
using System.Threading.Tasks;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;

namespace TSRDownloader.TestHelper;

/// <summary>
/// Deterministic, event-driven completion for the background workers of
/// <see cref="IDownloadService"/> and <see cref="IArchiveExtractionService"/>.
/// The services raise their change events only after a unit of work has fully
/// finished (inside a <c>finally</c>), so completing off those events avoids the
/// races that polling the mutable status field would introduce.
/// </summary>
public static class ServiceCompletion
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Completes once <paramref name="condition"/> holds after an extraction event.</summary>
    public static Task WaitAsync(
        IArchiveExtractionService service, Func<bool> condition, TimeSpan? timeout = null)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnItemChanged(DownloadItem _) => TryComplete();
        void OnStatisticsChanged() => TryComplete();
        void TryComplete()
        {
            if (!condition())
                return;
            service.ItemChanged -= OnItemChanged;
            service.StatisticsChanged -= OnStatisticsChanged;
            completion.TrySetResult();
        }

        service.ItemChanged += OnItemChanged;
        service.StatisticsChanged += OnStatisticsChanged;
        TryComplete(); // in case the condition already holds

        return completion.Task.WaitAsync(timeout ?? DefaultTimeout);
    }

    /// <summary>Completes once <paramref name="condition"/> holds after a download event.</summary>
    public static Task WaitAsync(
        IDownloadService service, Func<bool> condition, TimeSpan? timeout = null)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnItemChanged(DownloadItem _) => TryComplete();
        void OnStatisticsChanged() => TryComplete();
        void TryComplete()
        {
            if (!condition())
                return;
            service.ItemChanged -= OnItemChanged;
            service.QueueStatisticsChanged -= OnStatisticsChanged;
            completion.TrySetResult();
        }

        service.ItemChanged += OnItemChanged;
        service.QueueStatisticsChanged += OnStatisticsChanged;
        TryComplete(); // in case the condition already holds

        return completion.Task.WaitAsync(timeout ?? DefaultTimeout);
    }
}
