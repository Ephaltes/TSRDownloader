using System.Runtime.InteropServices;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Keeps the app to one running instance per user session. Two instances would both watch the
/// clipboard and download the same items into the same files. A second start signals the first
/// one (which then shows its window) and exits.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const int AllowAnyProcess = -1;

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _showSignal;
    private RegisteredWaitHandle? _registration;

    /// <summary>Whether this process is the first (and only) instance.</summary>
    public bool IsFirstInstance { get; }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

    public SingleInstance(string name)
    {
        _mutex = new Mutex(initiallyOwned: true, $@"Local\{name}", out bool createdNew);
        IsFirstInstance = createdNew;
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\{name}.Show");
    }

    /// <summary>Asks the first instance to show its window.</summary>
    public void SignalFirstInstance()
    {
        // Windows only lets the first instance take the foreground if we, the active process, allow it.
        AllowSetForegroundWindow(AllowAnyProcess);
        _showSignal.Set();
    }

    /// <summary>Invokes <paramref name="onShowRequested"/> (on a pool thread) whenever another instance starts.</summary>
    public void ListenForSecondInstances(Action onShowRequested) =>
        _registration = ThreadPool.RegisterWaitForSingleObject(
            _showSignal, (_, _) => onShowRequested(), null, Timeout.Infinite, executeOnlyOnce: false);

    public void Dispose()
    {
        _registration?.Unregister(null);
        _showSignal.Dispose();
        _mutex.Dispose();
    }
}
