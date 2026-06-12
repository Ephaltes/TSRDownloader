using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TSRDownloader.UI.Localization;

/// <summary>
/// Provides localized strings from the embedded <c>Strings</c> resources and supports
/// switching the UI language at runtime. XAML binds to the indexer via the
/// <see cref="LocExtension"/>; raising <c>Item[]</c> on a language change refreshes all
/// of those bindings live.
/// </summary>
public partial class LocalizationManager : ObservableObject
{
    /// <summary>Shared instance used by the <see cref="LocExtension"/> markup extension.</summary>
    public static LocalizationManager Instance { get; } = new();

    private readonly ResourceManager _resourceManager =
        new("TSRDownloader.UI.Resources.Strings", typeof(LocalizationManager).Assembly);

    /// <summary>Used until a language is set, and for an empty code; matches the AppConfig default.</summary>
    private const string DefaultLanguage = "en";

    private CultureInfo _culture = CultureInfo.GetCultureInfo(DefaultLanguage);

    /// <summary>The current two-letter language code (e.g. "de", "en").</summary>
    public string CurrentLanguage => _culture.TwoLetterISOLanguageName;

    /// <summary>Raised after the language changes, for consumers that cache resolved strings.</summary>
    public event Action? LanguageChanged;

    /// <summary>Indexer used by XAML bindings: returns the localized string for <paramref name="key"/>.</summary>
    public string this[string key] => _resourceManager.GetString(key, _culture) ?? $"!{key}!";

    /// <summary>Returns the localized string for <paramref name="key"/>.</summary>
    public string Get(string key) => this[key];

    /// <summary>Returns the localized format string for <paramref name="key"/> with arguments applied.</summary>
    public string Format(string key, params object[] args) =>
        string.Format(_culture, this[key], args);

    /// <summary>Switches the active language (by two-letter code) and notifies bindings.</summary>
    public void SetLanguage(string twoLetterCode)
    {
        if (string.IsNullOrWhiteSpace(twoLetterCode))
            twoLetterCode = DefaultLanguage;

        if (string.Equals(CurrentLanguage, twoLetterCode, StringComparison.OrdinalIgnoreCase))
            return;

        _culture = CultureInfo.GetCultureInfo(twoLetterCode);
        CultureInfo.CurrentUICulture = _culture;
        CultureInfo.DefaultThreadCurrentUICulture = _culture;

        OnPropertyChanged("Item[]");
        LanguageChanged?.Invoke();
    }
}
