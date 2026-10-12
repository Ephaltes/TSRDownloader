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

/// <summary>
/// Simulates an application restart: one service stack saves its queue on shutdown,
/// a fresh stack restores it from disk and finishes the work.
/// </summary>
public class QueuePersistenceTests
{
    private static ConfigurationService ConfigurationService(string downloadDirectory, string configurationPath)
    {
        IConfiguration hostConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfig:DownloadDirectory"] = downloadDirectory,
                ["AppConfig:ExtractZipArchives"] = "true",
                ["AppConfig:DeleteZipAfterExtraction"] = "true"
            })
            .Build();
        return new ConfigurationService(hostConfiguration, NullLogger<ConfigurationService>.Instance, configurationPath);
    }

    [Fact]
    public async Task Restore_Should_FinishQueuedDownloads_When_AppRestarts()
    {
        // Arrange — first run: two links are queued but the app closes before downloading.
        using TemporaryDirectory temporaryDirectory = new();
        string downloadDirectory = temporaryDirectory.Combine("downloads");
        string queueFile = temporaryDirectory.Combine("data", "queue.json");
        string sourceZip = ZipTestBuilder.Create(temporaryDirectory.Combine("source", "mod.zip"),
            new Dictionary<string, string> { ["a.package"] = "data" });
        ConfigurationService configuration = ConfigurationService(downloadDirectory, temporaryDirectory.Combine("data", "settings.json"));
        RecordingConflictResolver resolver = new(new FileConflictResolution(FileConflictAction.Overwrite, false));

        using (ArchiveExtractionService extraction = new(new ZipArchiveExtractor(), configuration, resolver))
        using (DownloadService downloads = new(new FakeTsrDownloadClient(sourceZip, "mod.zip"), configuration, extraction))
        using (new DownloadQueueAutosave(downloads, extraction,
                   new DownloadQueueStore(queueFile, NullLogger<DownloadQueueStore>.Instance)))
        {
            downloads.EnqueueRange([new DownloadItem { ItemId = 1, Url = "u1" }]);
        }

        // Act — second run restores from disk and continues.
        DownloadQueueStore store = new(queueFile, NullLogger<DownloadQueueStore>.Instance);
        IReadOnlyList<DownloadItem> restored = store.Load();
        using ArchiveExtractionService extraction2 = new(new ZipArchiveExtractor(), configuration, resolver);
        using DownloadService downloads2 = new(new FakeTsrDownloadClient(sourceZip, "mod.zip"), configuration, extraction2);
        downloads2.Restore(restored);
        downloads2.Start();
        await ServiceCompletion.WaitAsync(extraction2,
            () => restored[0].Status == DownloadStatus.ExtractionCompleted);
        downloads2.Stop();

        // Assert
        restored.Count.ShouldBe(1);
        restored[0].Url.ShouldBe("u1");
        File.ReadAllText(Path.Combine(downloadDirectory, "mod", "a.package")).ShouldBe("data");
    }
}
