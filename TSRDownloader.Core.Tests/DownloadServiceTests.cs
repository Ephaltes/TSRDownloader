using System;
using System.IO;
using System.Linq;
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

public class DownloadServiceTests
{
    private static IConfigService Config(string dir, bool extract)
    {
        IConfigService config = Substitute.For<IConfigService>();
        config.Config.Returns(new AppConfig
        {
            DownloadDirectory = dir,
            ExtractZipArchives = extract,
            MaxActiveDownloads = 1
        });
        return config;
    }

    private static ITsrDownloadClient ClientReturning(string fileName)
    {
        ITsrDownloadClient client = Substitute.For<ITsrDownloadClient>();
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new TsrDownloadInfo(callInfo.ArgAt<int>(0), "url", fileName));
        client.DownloadFileAsync(Arg.Any<TsrDownloadInfo>(), Arg.Any<string>(),
                Arg.Any<Action<DownloadProgress>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Path.Combine(callInfo.ArgAt<string>(1), fileName)));
        return client;
    }

    [Fact]
    public async Task Start_Should_RouteToExtraction_When_DownloadedFileIsZipAndExtractionEnabled()
    {
        // Arrange
        using TempDir temp = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("mod.zip"), Config(temp.Path, extract: true), extraction);
        DownloadItem item = new() { ItemId = 1, Url = "u" };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.WaitingToExtract);
        service.Stop();

        // Assert
        extraction.Received(1).Enqueue(item);
        item.FilePath.ShouldBe(Path.Combine(temp.Path, "mod.zip"));
    }

    [Fact]
    public async Task Start_Should_Complete_When_DownloadedFileIsNotZip()
    {
        // Arrange
        using TempDir temp = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("readme.txt"), Config(temp.Path, extract: true), extraction);
        DownloadItem item = new() { ItemId = 1 };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Completed);
        service.Stop();

        // Assert
        extraction.DidNotReceive().Enqueue(Arg.Any<DownloadItem>());
    }

    [Fact]
    public async Task Start_Should_Complete_When_ExtractionDisabled()
    {
        // Arrange
        using TempDir temp = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("mod.zip"), Config(temp.Path, extract: false), extraction);
        DownloadItem item = new() { ItemId = 1 };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Completed);
        service.Stop();

        // Assert
        extraction.DidNotReceive().Enqueue(Arg.Any<DownloadItem>());
    }

    [Fact]
    public async Task Start_Should_FailWithMessage_When_ClientThrows()
    {
        // Arrange
        using TempDir temp = new();
        ITsrDownloadClient client = Substitute.For<ITsrDownloadClient>();
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("nope"));
        using DownloadService service = new(client, Config(temp.Path, extract: true),
            Substitute.For<IArchiveExtractionService>());
        DownloadItem item = new() { ItemId = 1 };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Failed);
        service.Stop();

        // Assert
        item.ErrorMessage.ShouldBe("nope");
    }

    private static ITsrDownloadClient ClientBlockingUntil(Task release)
    {
        ITsrDownloadClient client = Substitute.For<ITsrDownloadClient>();
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await release.WaitAsync(callInfo.ArgAt<CancellationToken>(1));
                return new TsrDownloadInfo(callInfo.ArgAt<int>(0), "url", $"{callInfo.ArgAt<int>(0)}.txt");
            });
        client.DownloadFileAsync(Arg.Any<TsrDownloadInfo>(), Arg.Any<string>(),
                Arg.Any<Action<DownloadProgress>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Path.Combine(callInfo.ArgAt<string>(1), callInfo.ArgAt<TsrDownloadInfo>(0).FileName)));
        return client;
    }

    [Fact]
    public async Task Start_Should_RunDownloadsConcurrently_When_MaxActiveDownloadsAllows()
    {
        // Arrange
        using TempDir temp = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IConfigService config = Substitute.For<IConfigService>();
        config.Config.Returns(new AppConfig { DownloadDirectory = temp.Path, MaxActiveDownloads = 2 });
        using DownloadService service = new(ClientBlockingUntil(release.Task), config,
            Substitute.For<IArchiveExtractionService>());
        DownloadItem[] items = [new() { ItemId = 1 }, new() { ItemId = 2 }, new() { ItemId = 3 }];

        // Act
        service.EnqueueRange(items);
        service.Start();
        await ServiceCompletion.WaitAsync(service,
            () => items.Count(i => i.Status == DownloadStatus.Downloading) == 2);
        int downloadingWhileBlocked = items.Count(i => i.Status == DownloadStatus.Downloading);
        release.SetResult();
        await ServiceCompletion.WaitAsync(service, () => items.All(i => i.Status == DownloadStatus.Completed));
        service.Stop();

        // Assert
        downloadingWhileBlocked.ShouldBe(2);
    }

    [Fact]
    public async Task Start_Should_FailItem_When_ClientTimesOutWithoutStop()
    {
        // Arrange
        using TempDir temp = new();
        ITsrDownloadClient client = Substitute.For<ITsrDownloadClient>();
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timed out"));
        using DownloadService service = new(client, Config(temp.Path, extract: true),
            Substitute.For<IArchiveExtractionService>());
        DownloadItem item = new() { ItemId = 1 };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Failed);
        service.Stop();

        // Assert
        item.ErrorMessage.ShouldBe("timed out");
    }

    [Fact]
    public async Task Retry_Should_RequeueAndDownload_When_ItemFailed()
    {
        // Arrange
        using TempDir temp = new();
        int attempts = 0;
        ITsrDownloadClient client = ClientReturning("readme.txt");
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++attempts == 1
                ? Task.FromException<TsrDownloadInfo>(new InvalidOperationException("first"))
                : Task.FromResult(new TsrDownloadInfo(1, "url", "readme.txt")));
        using DownloadService service = new(client, Config(temp.Path, extract: true),
            Substitute.For<IArchiveExtractionService>());
        DownloadItem item = new() { ItemId = 1 };
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Failed);

        // Act
        bool retried = service.Retry(item);
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Completed);
        service.Stop();

        // Assert
        retried.ShouldBeTrue();
        item.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Remove_Should_DropItem_When_ItemIsNotActive()
    {
        // Arrange
        using TempDir temp = new();
        using DownloadService service = new(ClientReturning("a.txt"), Config(temp.Path, extract: true),
            Substitute.For<IArchiveExtractionService>());
        DownloadItem item = new() { ItemId = 1 };
        service.Enqueue(item);

        // Act
        service.Remove(item);

        // Assert
        service.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Restore_Should_RequeueInterruptedDownloadAndKeepHistory_When_NotStarted()
    {
        // Arrange
        using TempDir temp = new();
        using DownloadService service = new(ClientReturning("a.txt"), Config(temp.Path, extract: true),
            Substitute.For<IArchiveExtractionService>());
        DownloadItem interrupted = new() { ItemId = 1, Status = DownloadStatus.Downloading, Progress = 40 };
        DownloadItem done = new() { ItemId = 2, Status = DownloadStatus.Completed, Progress = 100 };
        DownloadItem failed = new() { ItemId = 3, Status = DownloadStatus.Failed, ErrorMessage = "x" };

        // Act
        service.Restore([interrupted, done, failed]);

        // Assert
        interrupted.Status.ShouldBe(DownloadStatus.Queued);
        interrupted.Progress.ShouldBe(0);
        done.Status.ShouldBe(DownloadStatus.Completed);
        failed.Status.ShouldBe(DownloadStatus.Failed);
        failed.ErrorMessage.ShouldBe("x");
        service.Items.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData(DownloadStatus.WaitingToExtract)]
    [InlineData(DownloadStatus.Extracting)]
    public void Restore_Should_ReturnArchiveToExtractionQueue_When_ArchiveStillExists(DownloadStatus savedStatus)
    {
        // Arrange
        using TempDir temp = new();
        string archive = temp.Combine("mod.zip");
        File.WriteAllText(archive, "zip");
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("a.txt"), Config(temp.Path, extract: true), extraction);
        DownloadItem item = new() { ItemId = 1, Status = savedStatus, FilePath = archive };

        // Act
        service.Restore([item]);

        // Assert
        item.Status.ShouldBe(DownloadStatus.WaitingToExtract);
        extraction.Received(1).Enqueue(item);
    }

    [Fact]
    public void Restore_Should_RequeueDownload_When_ArchiveToExtractIsMissing()
    {
        // Arrange
        using TempDir temp = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("a.txt"), Config(temp.Path, extract: true), extraction);
        DownloadItem item = new()
        {
            ItemId = 1, Status = DownloadStatus.WaitingToExtract, FilePath = temp.Combine("gone.zip")
        };

        // Act
        service.Restore([item]);

        // Assert
        item.Status.ShouldBe(DownloadStatus.Queued);
        extraction.DidNotReceive().Enqueue(Arg.Any<DownloadItem>());
    }

    [Fact]
    public async Task Start_Should_DownloadRestoredItem_When_ItWasInterrupted()
    {
        // Arrange
        using TempDir temp = new();
        using DownloadService service = new(ClientReturning("a.txt"), Config(temp.Path, extract: true),
            Substitute.For<IArchiveExtractionService>());
        DownloadItem item = new() { ItemId = 1, Status = DownloadStatus.Downloading };
        service.Restore([item]);

        // Act
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Completed);
        service.Stop();

        // Assert
        item.FilePath.ShouldBe(Path.Combine(temp.Path, "a.txt"));
    }
}
