using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.TestHelper;
using Xunit;

namespace TSRDownloader.E2E.Tests;

public class FullPipelineE2ETests
{
    private sealed class Harness : IDisposable
    {
        public TemporaryDirectory TemporaryDirectory { get; } = new();
        public string DownloadDirectory { get; }
        public ConfigurationService Configuration { get; }
        public DownloadService Downloads { get; }
        public ArchiveExtractionService Extraction { get; }
        public ClipboardService Clipboard { get; } = new();

        public Harness(byte[] zipBytes, string fileName, FileConflictResolution resolution)
        {
            DownloadDirectory = TemporaryDirectory.Combine("downloads");
            IConfiguration hostConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppConfig:DownloadDirectory"] = DownloadDirectory,
                    ["AppConfig:ExtractZipArchives"] = "true",
                    ["AppConfig:DeleteZipAfterExtraction"] = "true",
                    ["AppConfig:MaxActiveDownloads"] = "1"
                })
                .Build();
            Configuration = new ConfigurationService(hostConfiguration, NullLogger<ConfigurationService>.Instance, TemporaryDirectory.Combine("appsettings.json"));

            StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", fileName, zipBytes);
            TsrDownloadClient client = new(new HttpClient(handler), () => TimeSpan.Zero);

            Extraction = new ArchiveExtractionService(new ZipArchiveExtractor(), Configuration, new RecordingConflictResolver(resolution));
            Downloads = new DownloadService(client, Configuration, Extraction);
        }

        public void Dispose()
        {
            Downloads.Dispose();
            Extraction.Dispose();
            TemporaryDirectory.Dispose();
        }
    }

    [Fact]
    public async Task FullPipeline_Should_ExtractToDisk_When_ClipboardUrlProcessed()
    {
        // Arrange
        string srcZip = ZipTestBuilder.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip"),
            new Dictionary<string, string> { ["a.package"] = "payload" });
        byte[] zipBytes = ZipTestBuilder.ReadBytes(srcZip);
        using Harness harness = new(zipBytes, "mod.zip", new FileConflictResolution(FileConflictAction.Overwrite, false));

        // Act
        List<DownloadItem> items = harness.Clipboard.ProcessClipboardContent(
            "https://www.thesimsresource.com/downloads/details/id/1");
        harness.Downloads.EnqueueRange(items);
        harness.Downloads.Start();
        await ServiceCompletion.WaitAsync(harness.Extraction, () => items[0].Status == DownloadStatus.ExtractionCompleted);
        harness.Downloads.Stop();

        // Assert
        items.ShouldNotBeEmpty();
        File.ReadAllText(Path.Combine(harness.DownloadDirectory, "mod", "a.package")).ShouldBe("payload");
        File.Exists(Path.Combine(harness.DownloadDirectory, "mod.zip")).ShouldBeFalse();

        File.Delete(srcZip);
    }

    [Fact]
    public async Task FullPipeline_Should_KeepExistingFile_When_ResolverSkips()
    {
        // Arrange
        string srcZip = ZipTestBuilder.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip"),
            new Dictionary<string, string> { ["a.package"] = "new" });
        byte[] zipBytes = ZipTestBuilder.ReadBytes(srcZip);
        using Harness harness = new(zipBytes, "mod.zip", new FileConflictResolution(FileConflictAction.Skip, true));

        // Pre-create the conflicting file in the extraction subfolder.
        string outputDirectory = Path.Combine(harness.DownloadDirectory, "mod");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "a.package"), "old");

        // Act
        List<DownloadItem> items = harness.Clipboard.ProcessClipboardContent(
            "https://www.thesimsresource.com/downloads/details/id/1");
        harness.Downloads.EnqueueRange(items);
        harness.Downloads.Start();
        await ServiceCompletion.WaitAsync(harness.Extraction, () => items[0].Status == DownloadStatus.ExtractionCompleted);
        harness.Downloads.Stop();

        // Assert
        File.ReadAllText(Path.Combine(outputDirectory, "a.package")).ShouldBe("old");

        File.Delete(srcZip);
    }
}
