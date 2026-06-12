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
        public TempDir Temp { get; } = new();
        public string DownloadDir { get; }
        public ConfigService Config { get; }
        public DownloadService Downloads { get; }
        public ArchiveExtractionService Extraction { get; }
        public ClipboardService Clipboard { get; } = new();

        public Harness(byte[] zipBytes, string fileName, FileConflictResolution resolution)
        {
            DownloadDir = Temp.Combine("downloads");
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppConfig:DownloadDirectory"] = DownloadDir,
                    ["AppConfig:ExtractZipArchives"] = "true",
                    ["AppConfig:DeleteZipAfterExtraction"] = "true",
                    ["AppConfig:MaxActiveDownloads"] = "1"
                })
                .Build();
            Config = new ConfigService(configuration, NullLogger<ConfigService>.Instance, Temp.Combine("appsettings.json"));

            StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", fileName, zipBytes);
            TsrDownloadClient client = new(new HttpClient(handler), () => TimeSpan.Zero);

            Extraction = new ArchiveExtractionService(new ZipArchiveExtractor(), Config, new RecordingConflictResolver(resolution));
            Downloads = new DownloadService(client, Config, Extraction);
        }

        public void Dispose()
        {
            Downloads.Dispose();
            Extraction.Dispose();
            Temp.Dispose();
        }
    }

    [Fact]
    public async Task FullPipeline_Should_ExtractToDisk_When_ClipboardUrlProcessed()
    {
        // Arrange
        string srcZip = ZipTestBuilder.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip"),
            new Dictionary<string, string> { ["a.package"] = "payload" });
        byte[] zipBytes = ZipTestBuilder.ReadBytes(srcZip);
        using Harness h = new(zipBytes, "mod.zip", new FileConflictResolution(FileConflictAction.Overwrite, false));

        // Act
        List<DownloadItem> items = h.Clipboard.ProcessClipboardContent(
            "https://www.thesimsresource.com/downloads/details/id/1");
        h.Downloads.EnqueueRange(items);
        h.Downloads.Start();
        await ServiceCompletion.WaitAsync(h.Extraction, () => items[0].Status == DownloadStatus.ExtractionCompleted);
        h.Downloads.Stop();

        // Assert
        items.ShouldNotBeEmpty();
        File.ReadAllText(Path.Combine(h.DownloadDir, "mod", "a.package")).ShouldBe("payload");
        File.Exists(Path.Combine(h.DownloadDir, "mod.zip")).ShouldBeFalse();

        File.Delete(srcZip);
    }

    [Fact]
    public async Task FullPipeline_Should_KeepExistingFile_When_ResolverSkips()
    {
        // Arrange
        string srcZip = ZipTestBuilder.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip"),
            new Dictionary<string, string> { ["a.package"] = "new" });
        byte[] zipBytes = ZipTestBuilder.ReadBytes(srcZip);
        using Harness h = new(zipBytes, "mod.zip", new FileConflictResolution(FileConflictAction.Skip, true));

        // Pre-create the conflicting file in the extraction subfolder.
        string outDir = Path.Combine(h.DownloadDir, "mod");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "a.package"), "old");

        // Act
        List<DownloadItem> items = h.Clipboard.ProcessClipboardContent(
            "https://www.thesimsresource.com/downloads/details/id/1");
        h.Downloads.EnqueueRange(items);
        h.Downloads.Start();
        await ServiceCompletion.WaitAsync(h.Extraction, () => items[0].Status == DownloadStatus.ExtractionCompleted);
        h.Downloads.Stop();

        // Assert
        File.ReadAllText(Path.Combine(outDir, "a.package")).ShouldBe("old");

        File.Delete(srcZip);
    }
}
