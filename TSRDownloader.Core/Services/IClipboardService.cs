using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Service for monitoring clipboard content and detecting TSR URLs.
/// The UI layer provides the Windows-specific hook; this service handles URL parsing and validation.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Processes raw clipboard text and returns any newly detected TSR download items.
    /// </summary>
    /// <param name="clipboardText">The raw text from the clipboard.</param>
    /// <returns>List of newly detected download items (empty if none).</returns>
    List<DownloadItem> ProcessClipboardContent(string clipboardText);

    /// <summary>
    /// Records <paramref name="itemId"/> as already listed (e.g. restored from the saved queue),
    /// so copying its link again does not add a duplicate.
    /// </summary>
    void MarkSeen(int itemId);

    /// <summary>
    /// Forgets that <paramref name="itemId"/> was seen, so copying its link again re-adds it
    /// (e.g. after the item was removed from the list).
    /// </summary>
    void Forget(int itemId);
}
