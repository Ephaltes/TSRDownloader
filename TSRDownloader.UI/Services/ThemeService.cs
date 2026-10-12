using TSRDownloader.Core.Models;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Applies a theme in two parts: WPF's Fluent <see cref="ThemeMode"/> styles the built-in controls
/// (menus, scroll bars, title bar), and the app's palette (<c>Themes/Light.xaml</c> or
/// <c>Themes/Dark.xaml</c>) supplies the <c>App.*</c> brushes the app's own styles use.
/// </summary>
public sealed class ThemeService : IThemeService
{
    public void Apply(AppTheme theme)
    {
        Application app = Application.Current;
        app.ThemeMode = theme == AppTheme.Dark ? ThemeMode.Dark : ThemeMode.Light;

        ResourceDictionary palette = new()
        {
            Source = new Uri($"pack://application:,,,/TSRDownloader;component/Themes/{theme}.xaml")
        };

        Collection<ResourceDictionary> dictionaries = app.Resources.MergedDictionaries;
        int index = dictionaries.ToList().FindIndex(IsPalette);
        if (index >= 0)
            dictionaries[index] = palette;
        else
            dictionaries.Add(palette);
    }

    private static bool IsPalette(ResourceDictionary dictionary) =>
        dictionary.Source?.OriginalString is { } source
        && (source.EndsWith("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase)
            || source.EndsWith("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase));
}
