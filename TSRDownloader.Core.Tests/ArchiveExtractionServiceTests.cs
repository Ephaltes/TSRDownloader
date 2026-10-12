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
    private static IConfigurationService ConfigurationWith(bool deleteZipAfterExtraction)
    {
        IConfigurationService configuration = Substitute.For<IConfigurationService>();
        configuration.Configuration.Returns(new AppConfiguration { DeleteZipAfterExtraction = deleteZipAfterExtraction });
        return configuration;
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
        using TemporaryDirectory temporaryDirectory = new();
        IArchiveExtractor extractor = Substitute.For<IArchiveExtractor>();
        DownloadItem item = ItemWithFile(temporaryDirectory.Combine("a.zip"));
        using ArchiveExtractionService service = new(
            extractor, ConfigurationWith(deleteZipAfterExtraction: true), Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        item.Progress.ShouldBe(100);
        File.Exists(temporaryDirectory.Combine("a.zip")).ShouldBeFalse();
    }

    [Fact]
    public async Task Enqueue_Should_KeepZip_When_DeleteDisabled()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        DownloadItem item = ItemWithFile(temporaryDirectory.Combine("a.zip"));
        using ArchiveExtractionService service = new(
            Substitute.For<IArchiveExtractor>(), ConfigurationWith(deleteZipAfterExtraction: false),
            Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        File.Exists(temporaryDirectory.Combine("a.zip")).ShouldBeTrue();
    }

    [Fact]
    public async Task Enqueue_Should_FailWithMessage_When_ExtractorThrows()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        IArchiveExtractor extractor = Substitute.For<IArchiveExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IFileConflictResolver>(),
                Arg.Any<Action<double>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("boom"));
        DownloadItem item = ItemWithFile(temporaryDirectory.Combine("a.zip"));
        using ArchiveExtractionService service = new(
            extractor, ConfigurationWith(deleteZipAfterExtraction: true), Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionFailed);

        // Assert
        item.ErrorMessage.ShouldBe("boom");
        File.Exists(temporaryDirectory.Combine("a.zip")).ShouldBeTrue();
    }

    [Fact]
    public async Task Enqueue_Should_ExtractInOrder_When_MultipleArchivesQueued()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        ConcurrentQueue<string> order = new();
        IArchiveExtractor extractor = Substitute.For<IArchiveExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IFileConflictResolver>(),
                Arg.Any<Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                order.Enqueue(callInfo.ArgAt<string>(0));
                return Task.CompletedTask;
            });
        DownloadItem first = ItemWithFile(temporaryDirectory.Combine("first.zip"));
        DownloadItem second = ItemWithFile(temporaryDirectory.Combine("second.zip"));
        using ArchiveExtractionService service = new(
            extractor, ConfigurationWith(deleteZipAfterExtraction: false), Substitute.For<IFileConflictResolver>());

        // Act
        service.Enqueue(first);
        service.Enqueue(second);
        await ServiceCompletion.WaitAsync(service, () =>
            first.Status == DownloadStatus.ExtractionCompleted &&
            second.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        order.ShouldBe(new[] { temporaryDirectory.Combine("first.zip"), temporaryDirectory.Combine("second.zip") });
    }

    [Fact]
    public async Task Enqueue_Should_RaiseItemAndStatisticsEvents_When_ExtractionCompletes()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        DownloadItem item = ItemWithFile(temporaryDirectory.Combine("a.zip"));
        bool itemChanged = false, statsChanged = false;
        using ArchiveExtractionService service = new(
            Substitute.For<IArchiveExtractor>(), ConfigurationWith(deleteZipAfterExtraction: false),
            Substitute.For<IFileConflictResolver>());
        service.ItemChanged += _ => itemChanged = true;
        service.StatisticsChanged += () => statsChanged = true;

        // Act
        service.Enqueue(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.ExtractionCompleted);

        // Assert
        itemChanged.ShouldBeTrue();
        statsChanged.ShouldBeTrue();
    }
}
