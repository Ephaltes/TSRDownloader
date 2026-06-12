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
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnItem(DownloadItem _) => TryComplete();
        void OnStats() => TryComplete();
        void TryComplete()
        {
            if (!condition())
                return;
            service.ItemChanged -= OnItem;
            service.StatsChanged -= OnStats;
            tcs.TrySetResult();
        }

        service.ItemChanged += OnItem;
        service.StatsChanged += OnStats;
        TryComplete(); // in case the condition already holds

        return tcs.Task.WaitAsync(timeout ?? DefaultTimeout);
    }

    /// <summary>Completes once <paramref name="condition"/> holds after a download event.</summary>
    public static Task WaitAsync(
        IDownloadService service, Func<bool> condition, TimeSpan? timeout = null)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnItem(DownloadItem _) => TryComplete();
        void OnStats() => TryComplete();
        void TryComplete()
        {
            if (!condition())
                return;
            service.ItemChanged -= OnItem;
            service.QueueStatsChanged -= OnStats;
            tcs.TrySetResult();
        }

        service.ItemChanged += OnItem;
        service.QueueStatsChanged += OnStats;
        TryComplete(); // in case the condition already holds

        return tcs.Task.WaitAsync(timeout ?? DefaultTimeout);
    }
}
