using System;
using System.Collections.Generic;
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
    private static IConfigurationService ConfigurationFor(string directory, bool extract)
    {
        IConfigurationService configuration = Substitute.For<IConfigurationService>();
        configuration.Configuration.Returns(new AppConfiguration
        {
            DownloadDirectory = directory,
            ExtractZipArchives = extract,
            MaxActiveDownloads = 1
        });
        return configuration;
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
        using TemporaryDirectory temporaryDirectory = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("mod.zip"), ConfigurationFor(temporaryDirectory.Path, extract: true), extraction);
        DownloadItem item = new() { ItemId = 1, Url = "u" };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.WaitingToExtract);
        service.Stop();

        // Assert
        extraction.Received(1).Enqueue(item);
        item.FilePath.ShouldBe(Path.Combine(temporaryDirectory.Path, "mod.zip"));
    }

    [Fact]
    public async Task Start_Should_Complete_When_DownloadedFileIsNotZip()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("readme.txt"), ConfigurationFor(temporaryDirectory.Path, extract: true), extraction);
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
        using TemporaryDirectory temporaryDirectory = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("mod.zip"), ConfigurationFor(temporaryDirectory.Path, extract: false), extraction);
        DownloadItem item = new() { ItemId = 1 };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Completed);
        service.Stop();

        // Assert
        extraction.DidNotReceive().Enqueue(Arg.Any<DownloadItem>());
    }

    private static readonly Func<int, TimeSpan> NoRetryDelay = _ => TimeSpan.Zero;

    [Fact]
    public async Task Start_Should_RetryAutomatically_When_DownloadFailsTransiently()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        int attempts = 0;
        ITsrDownloadClient client = ClientReturning("readme.txt");
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++attempts < 3
                ? Task.FromException<TsrDownloadInfo>(new InvalidOperationException("flaky"))
                : Task.FromResult(new TsrDownloadInfo(1, "url", "readme.txt")));
        List<int> retryDelaysRequested = [];
        using DownloadService service = new(client, ConfigurationFor(temporaryDirectory.Path, extract: true),
            Substitute.For<IArchiveExtractionService>(), attempt =>
            {
                retryDelaysRequested.Add(attempt);
                return TimeSpan.Zero;
            });
        DownloadItem item = new() { ItemId = 1 };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Completed);
        service.Stop();

        // Assert
        attempts.ShouldBe(3);
        retryDelaysRequested.ShouldBe([1, 2]);
        item.RetryAttempt.ShouldBe(0);
    }

    [Fact]
    public async Task Start_Should_Fail_When_AllAutomaticRetriesFail()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        ITsrDownloadClient client = Substitute.For<ITsrDownloadClient>();
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("down"));
        using DownloadService service = new(client, ConfigurationFor(temporaryDirectory.Path, extract: true),
            Substitute.For<IArchiveExtractionService>(), NoRetryDelay);
        DownloadItem item = new() { ItemId = 1 };

        // Act
        service.Enqueue(item);
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Failed);
        service.Stop();

        // Assert
        await client.Received(1 + DownloadService.MaxAutomaticRetries)
            .ResolveDownloadAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DefaultRetryDelay_Should_Increase_When_AttemptsIncrease()
    {
        // Act & Assert
        DownloadService.DefaultRetryDelay(1).ShouldBe(TimeSpan.FromSeconds(5));
        DownloadService.DefaultRetryDelay(2).ShouldBe(TimeSpan.FromSeconds(15));
        DownloadService.DefaultRetryDelay(3).ShouldBe(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task Start_Should_FailWithMessage_When_ClientThrows()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        ITsrDownloadClient client = Substitute.For<ITsrDownloadClient>();
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("nope"));
        using DownloadService service = new(client, ConfigurationFor(temporaryDirectory.Path, extract: true),
            Substitute.For<IArchiveExtractionService>(), NoRetryDelay);
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
        using TemporaryDirectory temporaryDirectory = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IConfigurationService configuration = Substitute.For<IConfigurationService>();
        configuration.Configuration.Returns(new AppConfiguration { DownloadDirectory = temporaryDirectory.Path, MaxActiveDownloads = 2 });
        using DownloadService service = new(ClientBlockingUntil(release.Task), configuration,
            Substitute.For<IArchiveExtractionService>());
        DownloadItem[] items = [new() { ItemId = 1 }, new() { ItemId = 2 }, new() { ItemId = 3 }];

        // Act
        service.EnqueueRange(items);
        service.Start();
        await ServiceCompletion.WaitAsync(service,
            () => items.Count(item => item.Status == DownloadStatus.Downloading) == 2);
        int downloadingWhileBlocked = items.Count(item => item.Status == DownloadStatus.Downloading);
        release.SetResult();
        await ServiceCompletion.WaitAsync(service, () => items.All(item => item.Status == DownloadStatus.Completed));
        service.Stop();

        // Assert
        downloadingWhileBlocked.ShouldBe(2);
    }

    [Fact]
    public async Task Start_Should_FailItem_When_ClientTimesOutWithoutStop()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        ITsrDownloadClient client = Substitute.For<ITsrDownloadClient>();
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timed out"));
        using DownloadService service = new(client, ConfigurationFor(temporaryDirectory.Path, extract: true),
            Substitute.For<IArchiveExtractionService>(), NoRetryDelay);
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
        using TemporaryDirectory temporaryDirectory = new();
        int attempts = 0;
        ITsrDownloadClient client = ClientReturning("readme.txt");
        client.ResolveDownloadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++attempts <= 1 + DownloadService.MaxAutomaticRetries
                ? Task.FromException<TsrDownloadInfo>(new InvalidOperationException("first"))
                : Task.FromResult(new TsrDownloadInfo(1, "url", "readme.txt")));
        using DownloadService service = new(client, ConfigurationFor(temporaryDirectory.Path, extract: true),
            Substitute.For<IArchiveExtractionService>(), NoRetryDelay);
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
        using TemporaryDirectory temporaryDirectory = new();
        using DownloadService service = new(ClientReturning("a.txt"), ConfigurationFor(temporaryDirectory.Path, extract: true),
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
        using TemporaryDirectory temporaryDirectory = new();
        using DownloadService service = new(ClientReturning("a.txt"), ConfigurationFor(temporaryDirectory.Path, extract: true),
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
        using TemporaryDirectory temporaryDirectory = new();
        string archive = temporaryDirectory.Combine("mod.zip");
        File.WriteAllText(archive, "zip");
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("a.txt"), ConfigurationFor(temporaryDirectory.Path, extract: true), extraction);
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
        using TemporaryDirectory temporaryDirectory = new();
        IArchiveExtractionService extraction = Substitute.For<IArchiveExtractionService>();
        using DownloadService service = new(ClientReturning("a.txt"), ConfigurationFor(temporaryDirectory.Path, extract: true), extraction);
        DownloadItem item = new()
        {
            ItemId = 1, Status = DownloadStatus.WaitingToExtract, FilePath = temporaryDirectory.Combine("gone.zip")
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
        using TemporaryDirectory temporaryDirectory = new();
        using DownloadService service = new(ClientReturning("a.txt"), ConfigurationFor(temporaryDirectory.Path, extract: true),
            Substitute.For<IArchiveExtractionService>());
        DownloadItem item = new() { ItemId = 1, Status = DownloadStatus.Downloading };
        service.Restore([item]);

        // Act
        service.Start();
        await ServiceCompletion.WaitAsync(service, () => item.Status == DownloadStatus.Completed);
        service.Stop();

        // Assert
        item.FilePath.ShouldBe(Path.Combine(temporaryDirectory.Path, "a.txt"));
    }
}
