using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Encapsulates the The Sims Resource download protocol (ticket negotiation, URL
/// resolution and the actual file transfer with resume support).
/// This keeps the HTTP/protocol details out of <see cref="IDownloadService"/>,
/// which is only concerned with queueing and concurrency.
/// </summary>
public interface ITsrDownloadClient
{
    /// <summary>
    /// Performs the TSR ticket handshake and resolves the final download URL and
    /// file name for the given item.
    /// </summary>
    Task<TsrDownloadInfo> ResolveDownloadAsync(int itemId, CancellationToken ct);

    /// <summary>
    /// Downloads the file described by <paramref name="info"/> into
    /// <paramref name="destinationDirectory"/>, resuming from a partial
    /// <c>.part</c> file if one exists. Reports byte progress via
    /// <paramref name="onProgress"/> and returns the final file path.
    /// </summary>
    Task<string> DownloadFileAsync(
        TsrDownloadInfo info,
        string destinationDirectory,
        Action<DownloadProgress>? onProgress,
        CancellationToken ct);
}
