using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Persists the download list between application runs.
/// </summary>
public interface IDownloadQueueStore
{
    /// <summary>Returns the saved items, or an empty list if nothing (readable) was saved.</summary>
    IReadOnlyList<DownloadItem> Load();

    /// <summary>Replaces the saved list with <paramref name="items"/>.</summary>
    void Save(IReadOnlyList<DownloadItem> items);
}
