using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.TestHelper;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class ConfigurationServiceTests
{
    private static ConfigurationService NewService(string configurationPath, IDictionary<string, string?>? initial = null)
    {
        IConfiguration hostConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(initial ?? new Dictionary<string, string?>())
            .Build();
        return new ConfigurationService(hostConfiguration, NullLogger<ConfigurationService>.Instance, configurationPath);
    }

    [Fact]
    public void Update_Should_RoundTripConfiguration_When_ReloadedFromDisk()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string path = temporaryDirectory.Combine("appsettings.json");
        ConfigurationService writer = NewService(path);

        // Act
        writer.Update(new AppConfiguration
        {
            DownloadDirectory = @"C:\dl",
            MaxActiveDownloads = 5,
            ExtractZipArchives = false,
            DeleteZipAfterExtraction = false
        });

        ConfigurationService reader = NewService(path);
        reader.Load();

        // Assert
        File.Exists(path).ShouldBeTrue();
        reader.Configuration.MaxActiveDownloads.ShouldBe(5);
        reader.Configuration.ExtractZipArchives.ShouldBeFalse();
        reader.Configuration.DeleteZipAfterExtraction.ShouldBeFalse();
        reader.Configuration.DownloadDirectory.ShouldBe(@"C:\dl");
    }

    [Fact]
    public void Update_Should_RoundTripWindowLayout_When_ReloadedFromDisk()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string path = temporaryDirectory.Combine("settings.json");

        // Act
        NewService(path).Update(new AppConfiguration
        {
            MainWindowLayout = new WindowLayout
            {
                Left = 10, Top = 20, Width = 1000, Height = 600, IsMaximized = true, ColumnWidths = [64, 70, 300]
            }
        });
        ConfigurationService reader = NewService(path);
        reader.Load();

        // Assert
        WindowLayout layout = reader.Configuration.MainWindowLayout.ShouldNotBeNull();
        (layout.Left, layout.Top, layout.Width, layout.Height, layout.IsMaximized).ShouldBe((10d, 20d, 1000d, 600d, true));
        layout.ColumnWidths.ShouldBe([64, 70, 300]);
    }

    [Theory]
    [InlineData(100, 100, true)]      // on the screen
    [InlineData(-3000, 100, false)]   // on a monitor that is gone
    [InlineData(100, 2000, false)]    // below the screen
    [InlineData(-30, 0, true)]        // slightly off the left edge, title bar still reachable
    public void IsReachableOn_Should_RequireTheTitleBarOnScreen_When_Checked(double left, double top, bool expected)
    {
        // Arrange
        WindowLayout layout = new() { Left = left, Top = top, Width = 900, Height = 500 };

        // Act & Assert
        layout.IsReachableOn(0, 0, 1920, 1080).ShouldBe(expected);
    }

    [Fact]
    public void Load_Should_KeepDefaults_When_FileMissing()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        ConfigurationService service = NewService(temporaryDirectory.Combine("missing.json"));

        // Act
        service.Load();

        // Assert
        service.Configuration.ExtractZipArchives.ShouldBeTrue();
        service.Configuration.DeleteZipAfterExtraction.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Should_NormalizeDownloadDirectory_When_ConfiguredValueIsEmpty()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();

        // Act
        ConfigurationService service = NewService(temporaryDirectory.Combine("appsettings.json"), new Dictionary<string, string?>
        {
            ["AppConfig:DownloadDirectory"] = ""
        });

        // Assert
        service.Configuration.DownloadDirectory.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Update_Should_PersistUpdateModeAsString_When_Saved()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string path = temporaryDirectory.Combine("settings.json");
        ConfigurationService writer = NewService(path);

        // Act
        writer.Update(new AppConfiguration { UpdateMode = UpdateMode.NotifyOnly });
        ConfigurationService reader = NewService(path);
        reader.Load();

        // Assert
        File.ReadAllText(path).ShouldContain("\"UpdateMode\": \"NotifyOnly\"");
        reader.Configuration.UpdateMode.ShouldBe(UpdateMode.NotifyOnly);
    }

    [Fact]
    public void Load_Should_DefaultToAutomaticUpdates_When_UpdateModeMissing()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string path = temporaryDirectory.Combine("settings.json");
        File.WriteAllText(path, """{ "AppConfig": { "MaxActiveDownloads": 4 } }""");
        ConfigurationService service = NewService(path);

        // Act
        service.Load();

        // Assert
        service.Configuration.MaxActiveDownloads.ShouldBe(4);
        service.Configuration.UpdateMode.ShouldBe(UpdateMode.Automatic);
    }

    [Fact]
    public void Load_Should_ReadUpdateMode_When_StoredAsNumber()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string path = temporaryDirectory.Combine("settings.json");
        File.WriteAllText(path, """{ "AppConfig": { "UpdateMode": 2 } }""");
        ConfigurationService service = NewService(path);

        // Act
        service.Load();

        // Assert
        service.Configuration.UpdateMode.ShouldBe(UpdateMode.Disabled);
    }

    [Fact]
    public void Constructor_Should_BindUpdateMode_When_ConfiguredAsString()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();

        // Act
        ConfigurationService service = NewService(temporaryDirectory.Combine("settings.json"), new Dictionary<string, string?>
        {
            ["AppConfig:UpdateMode"] = "Disabled"
        });

        // Assert
        service.Configuration.UpdateMode.ShouldBe(UpdateMode.Disabled);
    }

    [Fact]
    public void Load_Should_ClampAndDefaultValues_When_FileHasInvalidValues()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string path = temporaryDirectory.Combine("settings.json");
        File.WriteAllText(path,
            "{\"AppConfig\":{\"DownloadDirectory\":\"\",\"MaxActiveDownloads\":0,\"TicketActivationDelaySeconds\":-5}}");
        ConfigurationService service = NewService(path);

        // Act
        service.Load();

        // Assert
        service.Configuration.DownloadDirectory.ShouldNotBeNullOrWhiteSpace();
        service.Configuration.MaxActiveDownloads.ShouldBe(AppConfiguration.MinActiveDownloads);
        service.Configuration.TicketActivationDelaySeconds.ShouldBe(AppConfiguration.MinTicketActivationDelaySeconds);
    }

    [Fact]
    public void Save_Should_LeaveNoTemporaryFile_When_Written()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string path = temporaryDirectory.Combine("settings.json");
        ConfigurationService service = NewService(path);

        // Act
        service.Save();

        // Assert
        Directory.GetFiles(temporaryDirectory.Path).ShouldBe(new[] { path });
    }
}
