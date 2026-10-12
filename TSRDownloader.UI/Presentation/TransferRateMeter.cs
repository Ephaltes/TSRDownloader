namespace TSRDownloader.UI.Presentation;

/// <summary>
/// Estimates a download's speed from (bytes, time) samples. Measures over windows of at least
/// a second and smooths between windows, so the displayed speed and time left don't jitter.
/// </summary>
public sealed class TransferRateMeter
{
    private const long WindowMs = 1000;
    private const double Smoothing = 0.3;

    private long _windowStartMs = -1;
    private long _windowStartBytes;

    /// <summary>Bytes per second, or null until a full window has been measured.</summary>
    public double? BytesPerSecond { get; private set; }

    public void Sample(long bytes, long nowMs)
    {
        // First sample, or the download restarted from fewer bytes: start measuring again.
        if (_windowStartMs < 0 || bytes < _windowStartBytes)
        {
            Reset();
            _windowStartMs = nowMs;
            _windowStartBytes = bytes;
            return;
        }

        long elapsedMs = nowMs - _windowStartMs;
        if (elapsedMs < WindowMs)
            return;

        double rate = (bytes - _windowStartBytes) * 1000.0 / elapsedMs;
        BytesPerSecond = BytesPerSecond is { } previous
            ? previous + Smoothing * (rate - previous)
            : rate;
        _windowStartMs = nowMs;
        _windowStartBytes = bytes;
    }

    public void Reset()
    {
        _windowStartMs = -1;
        BytesPerSecond = null;
    }
}
