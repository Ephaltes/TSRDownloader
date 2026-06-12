using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using Shouldly;
using TSRDownloader.UI.Localization;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class LocalizationManagerTests
{
    [Fact]
    public void Get_Should_ReturnLocalizedValue_When_KeyExists()
    {
        // Arrange
        LocalizationManager loc = LocalizationManager.Instance;
        loc.SetLanguage("en");

        // Act
        string value = loc.Get("Status_Completed");

        // Assert
        value.ShouldBe("Completed");
    }

    [Fact]
    public void Get_Should_ReturnMarkedKey_When_KeyMissing()
    {
        // Act
        string value = LocalizationManager.Instance.Get("No_Such_Key_12345");

        // Assert
        value.ShouldBe("!No_Such_Key_12345!");
    }

    [Fact]
    public void SetLanguage_Should_ChangeLanguageAndValues_When_GivenNewCode()
    {
        // Arrange
        LocalizationManager loc = LocalizationManager.Instance;

        // Act
        loc.SetLanguage("de");

        // Assert
        loc.CurrentLanguage.ShouldBe("de");
        loc.Get("Status_Completed").ShouldBe("Fertig");

        // Cleanup (shared singleton)
        loc.SetLanguage("en");
    }

    [Fact]
    public void Format_Should_ApplyArguments_When_KeyHasPlaceholders()
    {
        // Arrange
        LocalizationManager loc = LocalizationManager.Instance;
        loc.SetLanguage("en");

        // Act
        string formatted = loc.Format("Notify_UrlCopied", "mod.zip");

        // Assert
        formatted.ShouldBe("Copied URL for mod.zip");
    }

    [Fact]
    public void ResourceSets_Should_ContainSameKeys_When_ComparingEnglishAndGerman()
    {
        // Arrange
        ResourceManager resources = new("TSRDownloader.UI.Resources.Strings", typeof(LocalizationManager).Assembly);

        // Act
        HashSet<string> englishKeys = ReadKeys(resources, CultureInfo.InvariantCulture);
        HashSet<string> germanKeys = ReadKeys(resources, CultureInfo.GetCultureInfo("de"));

        // Assert
        englishKeys.Except(germanKeys).ShouldBeEmpty("keys missing in Strings.de.resx");
        germanKeys.Except(englishKeys).ShouldBeEmpty("keys missing in Strings.resx");
    }

    [Theory]
    [InlineData("en", "Toast_UpdateAvailable_Install", "Install now")]
    [InlineData("de", "Toast_UpdateAvailable_Install", "Jetzt installieren")]
    [InlineData("en", "Settings_UpdateMode_NotifyOnly", "Notify only")]
    [InlineData("de", "Settings_UpdateMode_NotifyOnly", "Nur benachrichtigen")]
    public void Get_Should_ReturnUpdateTexts_When_LanguageIsSet(string language, string key, string expected)
    {
        // Arrange
        LocalizationManager loc = LocalizationManager.Instance;
        loc.SetLanguage(language);

        // Act
        string value = loc.Get(key);

        // Assert
        value.ShouldBe(expected);

        // Cleanup (shared singleton)
        loc.SetLanguage("en");
    }

    private static HashSet<string> ReadKeys(ResourceManager resources, CultureInfo culture)
    {
        // tryParents: false — only the keys actually present in this culture's file.
        ResourceSet resourceSet = resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
                                  ?? throw new InvalidOperationException($"No resources for {culture.Name}");
        return resourceSet.Cast<DictionaryEntry>()
            .Select(entry => (string)entry.Key)
            .ToHashSet();
    }
}
