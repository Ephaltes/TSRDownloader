using TSRDownloader.Core.Models;

namespace TSRDownloader.UI.Localization;

/// <summary>
/// A selectable theme, shown in the settings dropdown.
/// </summary>
/// <param name="Theme">The theme stored in the configuration.</param>
/// <param name="DisplayName">Localized name shown to the user.</param>
public record ThemeOption(AppTheme Theme, string DisplayName);
