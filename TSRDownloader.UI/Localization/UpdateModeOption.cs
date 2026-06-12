using TSRDownloader.Core.Models;

namespace TSRDownloader.UI.Localization;

/// <summary>
/// A selectable update mode, shown in the settings dropdown.
/// </summary>
/// <param name="Mode">The update mode stored in the configuration.</param>
/// <param name="DisplayName">Localized name shown to the user.</param>
public record UpdateModeOption(UpdateMode Mode, string DisplayName);
