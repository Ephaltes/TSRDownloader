namespace TSRDownloader.UI.Localization;

/// <summary>
/// A selectable UI language, shown in the settings dropdown.
/// </summary>
/// <param name="Code">Two-letter language code (e.g. "de", "en").</param>
/// <param name="DisplayName">Human-readable name shown to the user (its own endonym).</param>
public record LanguageOption(string Code, string DisplayName);
