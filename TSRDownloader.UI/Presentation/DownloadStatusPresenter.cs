using TSRDownloader.Core.Models;

namespace TSRDownloader.UI.Presentation;

/// <summary>
/// The visual representation of a single <see cref="DownloadStatus"/>:
/// the icon, label resource key, accent colour and whether progress is indeterminate.
/// </summary>
/// <param name="LabelKey">Localization key resolved against the current UI language.</param>
public sealed record DownloadStatusPresentation(string Icon, string LabelKey, Brush Color, bool IsIndeterminate);

/// <summary>
/// Maps each <see cref="DownloadStatus"/> to its <see cref="DownloadStatusPresentation"/>.
/// Adding a new status requires only a single entry here (plus the enum value) —
/// no scattered switch statements to update.
/// </summary>
public static class DownloadStatusPresenter
{
    private static readonly DownloadStatusPresentation Unknown =
        new("❔", "Status_Unknown", FrozenBrush(Colors.Gray), true);

    private static readonly IReadOnlyDictionary<DownloadStatus, DownloadStatusPresentation> Map =
        new Dictionary<DownloadStatus, DownloadStatusPresentation>
        {
            [DownloadStatus.Queued] =
                new("⏳", "Status_Queued", FrozenBrush(Color.FromRgb(158, 158, 158)), IsIndeterminate: true),
            [DownloadStatus.Downloading] =
                new("⬇️", "Status_Downloading", FrozenBrush(Color.FromRgb(33, 150, 243)), IsIndeterminate: false),
            [DownloadStatus.Completed] =
                new("✅", "Status_Completed", FrozenBrush(Color.FromRgb(76, 175, 80)), IsIndeterminate: false),
            [DownloadStatus.Failed] =
                new("❌", "Status_Failed", FrozenBrush(Color.FromRgb(244, 67, 54)), IsIndeterminate: true),
            [DownloadStatus.WaitingToExtract] =
                new("📦", "Status_WaitingToExtract", FrozenBrush(Color.FromRgb(255, 167, 38)), IsIndeterminate: true),
            [DownloadStatus.Extracting] =
                new("🗜️", "Status_Extracting", FrozenBrush(Color.FromRgb(103, 58, 183)), IsIndeterminate: false),
            [DownloadStatus.ExtractionCompleted] =
                new("✅", "Status_ExtractionCompleted", FrozenBrush(Color.FromRgb(76, 175, 80)), IsIndeterminate: false),
            [DownloadStatus.ExtractionFailed] =
                new("⚠️", "Status_ExtractionFailed", FrozenBrush(Color.FromRgb(244, 67, 54)), IsIndeterminate: false),
        };

    /// <summary>Returns the presentation for the given status, or a neutral fallback.</summary>
    public static DownloadStatusPresentation For(DownloadStatus status) =>
        Map.TryGetValue(status, out DownloadStatusPresentation? presentation) ? presentation : Unknown;

    private static SolidColorBrush FrozenBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze(); // frozen brushes are immutable, shareable and thread-safe
        return brush;
    }
}
