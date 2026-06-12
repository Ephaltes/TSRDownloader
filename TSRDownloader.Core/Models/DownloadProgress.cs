namespace TSRDownloader.Core.Models;

/// <summary>
/// A point-in-time snapshot of a file download's byte progress.
/// </summary>
/// <param name="DownloadedBytes">Bytes written to disk so far (including resumed bytes).</param>
/// <param name="TotalBytes">Total expected size in bytes, or 0 if unknown.</param>
public readonly record struct DownloadProgress(long DownloadedBytes, long TotalBytes)
{
    /// <summary>Progress as a percentage (0–100), rounded to one decimal place.</summary>
    public double Percentage =>
        TotalBytes > 0 ? Math.Round((double)DownloadedBytes / TotalBytes * 100, 1) : 0;
}
