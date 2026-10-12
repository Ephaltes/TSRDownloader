using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.TestHelper;
using Xunit;

namespace TSRDownloader.Integration.Tests;

public class DownloadExtractionPipelineTests
{
    private static ConfigurationService ConfigurationService(string downloadDirectory, string configurationPath, bool deleteZipAfterExtraction)
    {
        IConfiguration hostConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfig:DownloadDirectory"] = downloadDirectory,
                ["AppConfig:ExtractZipArchives"] = "true",
                ["AppConfig:DeleteZipAfterExtraction"] = deleteZipAfterExtraction ? "true" : "false",
                ["AppConfig:MaxActiveDownloads"] = "1"
            })
            .Build();
        return new ConfigurationService(hostConfiguration, NullLogger<ConfigurationService>.Instance, configurationPath);
    }

    [Fact]
    public async Task Pipeline_Should_ExtractIntoSubfolderAndDeleteZip_When_ZipDownloaded()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string downloadDirectory = temporaryDirectory.Combine("downloads");
        string sourceZip = ZipTestBuilder.Create(temporaryDirectory.Combine("source", "mod.zip"),
            new Dictionary<string, string> { ["a.package"] = "data", ["nested/b.package"] = "more" });
        ConfigurationService configuration = ConfigurationService(downloadDirectory, temporaryDirectory.Combine("appsettings.json"), deleteZipAfterExtraction: true);
        RecordingConflictResolver resolver = new(new FileConflictResolution(FileConflictAction.Overwrite, false));
        using ArchiveExtractionService extraction = new(new ZipArchiveExtractor(), configuration, resolver);
        using DownloadService downloads = new(new FakeTsrDownloadClient(sourceZip, "mod.zip"), configuration, extraction);
        DownloadItem item = new() { ItemId = 1, Url = "u" };

        // Act
        downloads.Enqueue(item);
        downloads.Start();
        await ServiceCompletion.WaitAsync(extraction, () => item.Status == DownloadStatus.ExtractionCompleted);
        downloads.Stop();

        // Assert
        string outputDirectory = Path.Combine(downloadDirectory, "mod");
        File.ReadAllText(Path.Combine(outputDirectory, "a.package")).ShouldBe("data");
        File.ReadAllText(Path.Combine(outputDirectory, "nested", "b.package")).ShouldBe("more");
        File.Exists(Path.Combine(downloadDirectory, "mod.zip")).ShouldBeFalse();
    }

    [Fact]
    public async Task Pipeline_Should_ExtractZip_When_DeleteDisabled()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string downloadDirectory = temporaryDirectory.Combine("downloads");
        string sourceZip = ZipTestBuilder.Create(temporaryDirectory.Combine("source", "s.zip"),
            new Dictionary<string, string> { ["x.txt"] = "x" });
        ConfigurationService configuration = ConfigurationService(downloadDirectory, temporaryDirectory.Combine("appsettings.json"), deleteZipAfterExtraction: false);
        RecordingConflictResolver resolver = new(new FileConflictResolution(FileConflictAction.Overwrite, false));
        using ArchiveExtractionService extraction = new(new ZipArchiveExtractor(), configuration, resolver);
        using DownloadService downloads = new(new FakeTsrDownloadClient(sourceZip, "d1.zip"), configuration, extraction);
        DownloadItem item = new() { ItemId = 1 };

        // Act
        downloads.Enqueue(item);
        downloads.Start();
        await ServiceCompletion.WaitAsync(extraction, () => item.Status == DownloadStatus.ExtractionCompleted);
        downloads.Stop();

        // Assert
        File.Exists(Path.Combine(downloadDirectory, "d1", "x.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(downloadDirectory, "d1.zip")).ShouldBeTrue();
    }
}
