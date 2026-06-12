using System.Windows.Threading;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Production dispatcher backed by the WPF application dispatcher. Runs inline on the UI
/// thread; from other threads it queues the action instead of blocking the caller, so a
/// download or extraction worker never waits for the UI to repaint.
/// </summary>
public sealed class WpfDispatcher : IUiDispatcher
{
    public void Invoke(Action action)
    {
        // Null during shutdown: there is no UI left to update.
        Dispatcher? dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
            return;

        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(action);
    }
}
