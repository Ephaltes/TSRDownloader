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
    private static ConfigService ConfigService(string downloadDir, string configPath, bool deleteZip)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfig:DownloadDirectory"] = downloadDir,
                ["AppConfig:ExtractZipArchives"] = "true",
                ["AppConfig:DeleteZipAfterExtraction"] = deleteZip ? "true" : "false",
                ["AppConfig:MaxActiveDownloads"] = "1"
            })
            .Build();
        return new ConfigService(configuration, NullLogger<ConfigService>.Instance, configPath);
    }

    [Fact]
    public async Task Pipeline_Should_ExtractIntoSubfolderAndDeleteZip_When_ZipDownloaded()
    {
        // Arrange
        using TempDir temp = new();
        string downloadDir = temp.Combine("downloads");
        string sourceZip = ZipTestBuilder.Create(temp.Combine("source", "mod.zip"),
            new Dictionary<string, string> { ["a.package"] = "data", ["nested/b.package"] = "more" });
        ConfigService config = ConfigService(downloadDir, temp.Combine("appsettings.json"), deleteZip: true);
        RecordingConflictResolver resolver = new(new FileConflictResolution(FileConflictAction.Overwrite, false));
        using ArchiveExtractionService extraction = new(new ZipArchiveExtractor(), config, resolver);
        using DownloadService downloads = new(new FakeTsrDownloadClient(sourceZip, "mod.zip"), config, extraction);
        DownloadItem item = new() { ItemId = 1, Url = "u" };

        // Act
        downloads.Enqueue(item);
        downloads.Start();
        await ServiceCompletion.WaitAsync(extraction, () => item.Status == DownloadStatus.ExtractionCompleted);
        downloads.Stop();

        // Assert
        string outDir = Path.Combine(downloadDir, "mod");
        File.ReadAllText(Path.Combine(outDir, "a.package")).ShouldBe("data");
        File.ReadAllText(Path.Combine(outDir, "nested", "b.package")).ShouldBe("more");
        File.Exists(Path.Combine(downloadDir, "mod.zip")).ShouldBeFalse();
    }

    [Fact]
    public async Task Pipeline_Should_ExtractZip_When_DeleteDisabled()
    {
        // Arrange
        using TempDir temp = new();
        string downloadDir = temp.Combine("downloads");
        string sourceZip = ZipTestBuilder.Create(temp.Combine("source", "s.zip"),
            new Dictionary<string, string> { ["x.txt"] = "x" });
        ConfigService config = ConfigService(downloadDir, temp.Combine("appsettings.json"), deleteZip: false);
        RecordingConflictResolver resolver = new(new FileConflictResolution(FileConflictAction.Overwrite, false));
        using ArchiveExtractionService extraction = new(new ZipArchiveExtractor(), config, resolver);
        using DownloadService downloads = new(new FakeTsrDownloadClient(sourceZip, "d1.zip"), config, extraction);
        DownloadItem item = new() { ItemId = 1 };

        // Act
        downloads.Enqueue(item);
        downloads.Start();
        await ServiceCompletion.WaitAsync(extraction, () => item.Status == DownloadStatus.ExtractionCompleted);
        downloads.Stop();

        // Assert
        File.Exists(Path.Combine(downloadDir, "d1", "x.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(downloadDir, "d1.zip")).ShouldBeTrue();
    }
}
