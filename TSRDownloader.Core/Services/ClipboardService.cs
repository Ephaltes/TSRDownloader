using TSRDownloader.Core.Helpers;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Processes clipboard content to detect and de-duplicate TSR URLs.
/// </summary>
public class ClipboardService : IClipboardService
{
    private readonly HashSet<int> _seenItemIds = [];

    /// <inheritdoc />
    public List<DownloadItem> ProcessClipboardContent(string clipboardText)
    {
        List<DownloadItem> items = new();
        List<string> urls = TSRUrlParser.ExtractTSRUrls(clipboardText);

        foreach (string url in urls)
        {
            int? itemId = TSRUrlParser.ExtractItemId(url);
            if (itemId is null)
                continue;

            if (!_seenItemIds.Add(itemId.Value))
                continue;

            items.Add(new DownloadItem
            {
                ItemId = itemId.Value,
                Url = url,
                Status = DownloadStatus.Queued
            });
        }

        return items;
    }

    /// <inheritdoc />
    public void MarkSeen(int itemId) => _seenItemIds.Add(itemId);

    /// <inheritdoc />
    public void Forget(int itemId) => _seenItemIds.Remove(itemId);
}
