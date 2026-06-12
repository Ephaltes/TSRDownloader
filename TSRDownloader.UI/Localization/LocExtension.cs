using System.Windows.Markup;

namespace TSRDownloader.UI.Localization;

/// <summary>
/// XAML markup extension that binds a target property to a localized string by key,
/// e.g. <c>Text="{loc:Loc Settings_Save}"</c>. The binding refreshes automatically when
/// the language is switched at runtime via <see cref="LocalizationManager.SetLanguage"/>.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class LocExtension : MarkupExtension
{
    public LocExtension() { }

    public LocExtension(string key) => Key = key;

    /// <summary>The resource key to resolve.</summary>
    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        Binding binding = new($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
