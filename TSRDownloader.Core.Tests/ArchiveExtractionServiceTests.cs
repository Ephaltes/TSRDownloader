using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.TestHelper;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class ArchiveExtractionServiceTests
{
    private static IConfigService ConfigWith(bool deleteZip)
    {
        IConfigService config = Substitute.For<IConfigService>();
        config.Config.Returns(new AppConfig { DeleteZipAfterExtraction = deleteZip });
        return config;
    }

    private static DownloadItem ItemWithFile(string path)
    {
        File.WriteAllText(path, "zip");
        return new DownloadItem { ItemId = 1, FilePath = path, Status = DownloadStatus.WaitingToExtract };
    }

    [Fact]
    public async Task Enqueue_Should_CompleteAndDeleteZip_When_ExtractionSucceedsAndDeleteEnabled()
    {
        // Arrange
        using TempDir temp = new();
        IArchiveExtractor extractor = Substitute.For<IArchiveExtractor>();
        DownloadItem item = ItemWithFile(temp.Combine("a.zip"));
        using ArchiveExtractionService service = new(
            extractor, ConfigWith(deleteZip: true), Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        item.Progress.ShouldBe(100);
        File.Exists(temp.Combine("a.zip")).ShouldBeFalse();
    }

    [Fact]
    public async Task Enqueue_Should_KeepZip_When_DeleteDisabled()
    {
        // Arrange
        using TempDir temp = new();
        DownloadItem item = ItemWithFile(temp.Combine("a.zip"));
        using ArchiveExtractionService service = new(
            Substitute.For<IArchiveExtractor>(), ConfigWith(deleteZip: false),
            Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        File.Exists(temp.Combine("a.zip")).ShouldBeTrue();
    }

    [Fact]
    public async Task Enqueue_Should_FailWithMessage_When_ExtractorThrows()
    {
        // Arrange
        using TempDir temp = new();
        IArchiveExtractor extractor = Substitute.For<IArchiveExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IFileConflictResolver>(),
                Arg.Any<Action<double>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("boom"));
        DownloadItem item = ItemWithFile(temp.Combine("a.zip"));
        using ArchiveExtractionService service = new(
            extractor, ConfigWith(deleteZip: true), Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionFailed);

        // Assert
        item.ErrorMessage.ShouldBe("boom");
        File.Exists(temp.Combine("a.zip")).ShouldBeTrue();
    }

    [Fact]
    public async Task Enqueue_Should_ExtractInOrder_When_MultipleArchivesQueued()
    {
        // Arrange
        using TempDir temp = new();
        ConcurrentQueue<string> order = new();
        IArchiveExtractor extractor = Substitute.For<IArchiveExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IFileConflictResolver>(),
                Arg.Any<Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                order.Enqueue(callInfo.ArgAt<string>(0));
                return Task.CompletedTask;
            });
        DownloadItem first = ItemWithFile(temp.Combine("first.zip"));
        DownloadItem second = ItemWithFile(temp.Combine("second.zip"));
        using ArchiveExtractionService service = new(
            extractor, ConfigWith(deleteZip: false), Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(first);
        service.Enqueue(second);
        await ServiceCompletion.WaitAsync(service, () =>
            first.Status == DownloadStatus.ExtractionCompleted &&
            second.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        order.ShouldBe(new[] { temp.Combine("first.zip"), temp.Combine("second.zip") });
    }

    [Fact]
    public async Task Enqueue_Should_RaiseItemAndStatsEvents_When_ExtractionCompletes()
    {
        // Arrange
        using TempDir temp = new();
        DownloadItem item = ItemWithFile(temp.Combine("a.zip"));
        bool itemChanged = false, statsChanged = false;
        using ArchiveExtractionService service = new(
            Substitute.For<IArchiveExtractor>(), ConfigWith(deleteZip: false),
            Substitute.For<IFileConflictResolver>());
        service.ItemChanged += _ => itemChanged = true;
        service.StatsChanged += () => statsChanged = true;

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        itemChanged.ShouldBeTrue();
        statsChanged.ShouldBeTrue();
    }
}
