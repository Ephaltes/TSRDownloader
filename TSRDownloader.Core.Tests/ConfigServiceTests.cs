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

public class ConfigServiceTests
{
    private static ConfigService NewService(string configPath, IDictionary<string, string?>? initial = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(initial ?? new Dictionary<string, string?>())
            .Build();
        return new ConfigService(configuration, NullLogger<ConfigService>.Instance, configPath);
    }

    [Fact]
    public void Update_Should_RoundTripConfig_When_ReloadedFromDisk()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("appsettings.json");
        ConfigService writer = NewService(path);

        // Act
        writer.Update(new AppConfig
        {
            DownloadDirectory = @"C:\dl",
            MaxActiveDownloads = 5,
            ExtractZipArchives = false,
            DeleteZipAfterExtraction = false
        });

        ConfigService reader = NewService(path);
        reader.Load();

        // Assert
        File.Exists(path).ShouldBeTrue();
        reader.Config.MaxActiveDownloads.ShouldBe(5);
        reader.Config.ExtractZipArchives.ShouldBeFalse();
        reader.Config.DeleteZipAfterExtraction.ShouldBeFalse();
        reader.Config.DownloadDirectory.ShouldBe(@"C:\dl");
    }

    [Fact]
    public void Load_Should_KeepDefaults_When_FileMissing()
    {
        // Arrange
        using TempDir temp = new();
        ConfigService service = NewService(temp.Combine("missing.json"));

        // Act
        service.Load();

        // Assert
        service.Config.ExtractZipArchives.ShouldBeTrue();
        service.Config.DeleteZipAfterExtraction.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Should_NormalizeDownloadDirectory_When_ConfiguredValueIsEmpty()
    {
        // Arrange
        using TempDir temp = new();

        // Act
        ConfigService service = NewService(temp.Combine("appsettings.json"), new Dictionary<string, string?>
        {
            ["AppConfig:DownloadDirectory"] = ""
        });

        // Assert
        service.Config.DownloadDirectory.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Update_Should_PersistUpdateModeAsString_When_Saved()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("settings.json");
        ConfigService writer = NewService(path);

        // Act
        writer.Update(new AppConfig { UpdateMode = UpdateMode.NotifyOnly });
        ConfigService reader = NewService(path);
        reader.Load();

        // Assert
        File.ReadAllText(path).ShouldContain("\"UpdateMode\": \"NotifyOnly\"");
        reader.Config.UpdateMode.ShouldBe(UpdateMode.NotifyOnly);
    }

    [Fact]
    public void Load_Should_DefaultToAutomaticUpdates_When_UpdateModeMissing()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("settings.json");
        File.WriteAllText(path, """{ "AppConfig": { "MaxActiveDownloads": 4 } }""");
        ConfigService service = NewService(path);

        // Act
        service.Load();

        // Assert
        service.Config.MaxActiveDownloads.ShouldBe(4);
        service.Config.UpdateMode.ShouldBe(UpdateMode.Automatic);
    }

    [Fact]
    public void Load_Should_ReadUpdateMode_When_StoredAsNumber()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("settings.json");
        File.WriteAllText(path, """{ "AppConfig": { "UpdateMode": 2 } }""");
        ConfigService service = NewService(path);

        // Act
        service.Load();

        // Assert
        service.Config.UpdateMode.ShouldBe(UpdateMode.Disabled);
    }

    [Fact]
    public void Constructor_Should_BindUpdateMode_When_ConfiguredAsString()
    {
        // Arrange
        using TempDir temp = new();

        // Act
        ConfigService service = NewService(temp.Combine("settings.json"), new Dictionary<string, string?>
        {
            ["AppConfig:UpdateMode"] = "Disabled"
        });

        // Assert
        service.Config.UpdateMode.ShouldBe(UpdateMode.Disabled);
    }

    [Fact]
    public void Load_Should_ClampAndDefaultValues_When_FileHasInvalidValues()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("settings.json");
        File.WriteAllText(path,
            "{\"AppConfig\":{\"DownloadDirectory\":\"\",\"MaxActiveDownloads\":0,\"TicketActivationDelaySeconds\":-5}}");
        ConfigService service = NewService(path);

        // Act
        service.Load();

        // Assert
        service.Config.DownloadDirectory.ShouldNotBeNullOrWhiteSpace();
        service.Config.MaxActiveDownloads.ShouldBe(AppConfig.MinActiveDownloads);
        service.Config.TicketActivationDelaySeconds.ShouldBe(AppConfig.MinTicketActivationDelaySeconds);
    }

    [Fact]
    public void Save_Should_LeaveNoTempFile_When_Written()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("settings.json");
        ConfigService service = NewService(path);

        // Act
        service.Save();

        // Assert
        Directory.GetFiles(temp.Path).ShouldBe(new[] { path });
    }
}
