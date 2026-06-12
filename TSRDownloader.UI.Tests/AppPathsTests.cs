using System.IO;
using Shouldly;
using TSRDownloader.UI.Services;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class AppPathsTests
{
    [Fact]
    public void DataDirectory_Should_UseInstalledRoot_When_AppIsInstalled()
    {
        // Arrange / Act
        AppPaths paths = new(installedRootDirectory: @"C:\Users\me\AppData\Local\TSRDownloader",
            fallbackDirectory: @"C:\dev\bin");

        // Assert
        paths.DataDirectory.ShouldBe(@"C:\Users\me\AppData\Local\TSRDownloader");
        paths.SettingsFile.ShouldBe(@"C:\Users\me\AppData\Local\TSRDownloader\settings.json");
        paths.LogDirectory.ShouldBe(@"C:\Users\me\AppData\Local\TSRDownloader\logs");
    }

    [Fact]
    public void DataDirectory_Should_UseFallback_When_AppIsNotInstalled()
    {
        // Arrange / Act
        AppPaths paths = new(installedRootDirectory: null, fallbackDirectory: @"C:\dev\bin");

        // Assert
        paths.DataDirectory.ShouldBe(@"C:\dev\bin");
        paths.SettingsFile.ShouldBe(Path.Combine(@"C:\dev\bin", "settings.json"));
    }
}
