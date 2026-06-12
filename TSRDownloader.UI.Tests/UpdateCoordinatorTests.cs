using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;
using TSRDownloader.UI.ViewModels;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class UpdateCoordinatorTests
{
    private static readonly AvailableUpdate Update = new("1.1.7");

    private sealed class Fixture
    {
        public IUpdateService Updates { get; } = Substitute.For<IUpdateService>();
        public IConfigService Config { get; } = Substitute.For<IConfigService>();
        public IDownloadService Downloads { get; } = Substitute.For<IDownloadService>();
        public INotificationService Notifications { get; } = Substitute.For<INotificationService>();
        public IUpdateWindowService Windows { get; } = Substitute.For<IUpdateWindowService>();
        public List<UpdateViewModel> ShownWindows { get; } = [];

        public Fixture(UpdateMode mode, bool isInstalled = true)
        {
            Config.Config.Returns(new AppConfig { UpdateMode = mode });
            Updates.IsInstalled.Returns(isInstalled);
            Downloads.Items.Returns([]);
            Windows.When(windows => windows.Show(Arg.Any<UpdateViewModel>()))
                .Do(call => ShownWindows.Add(call.Arg<UpdateViewModel>()!));
        }

        public UpdateCoordinator Create(TimeSpan? checkTimeout = null)
        {
            LocalizationManager.Instance.SetLanguage("en");
            return new UpdateCoordinator(Updates, Config, Downloads, Notifications, Windows,
                LocalizationManager.Instance, new ImmediateDispatcher(),
                NullLogger<UpdateCoordinator>.Instance, checkTimeout);
        }
    }

    [Theory]
    [InlineData(UpdateMode.Disabled, true)]
    [InlineData(UpdateMode.NotifyOnly, true)]
    [InlineData(UpdateMode.Automatic, false)]
    public async Task RunStartupUpdateAsync_Should_NotCheck_When_NotAutomaticOrNotInstalled(UpdateMode mode, bool isInstalled)
    {
        // Arrange
        Fixture fixture = new(mode, isInstalled);
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.RunStartupUpdateAsync();

        // Assert
        applied.ShouldBeFalse();
        await fixture.Updates.DidNotReceive().CheckAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunStartupUpdateAsync_Should_ContinueWithoutDownload_When_NoUpdateAvailable()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.Automatic);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns((AvailableUpdate?)null);
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.RunStartupUpdateAsync();

        // Assert
        applied.ShouldBeFalse();
        await fixture.Updates.DidNotReceive().DownloadAsync(Arg.Any<AvailableUpdate>(), Arg.Any<Action<int>>(), Arg.Any<CancellationToken>());
        fixture.ShownWindows.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunStartupUpdateAsync_Should_DownloadThenApply_When_UpdateAvailable()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.Automatic);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        fixture.Updates.DownloadAsync(Update, Arg.Any<Action<int>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<Action<int>>()!(55);
                return Task.CompletedTask;
            });
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.RunStartupUpdateAsync();

        // Assert
        applied.ShouldBeTrue();
        Received.InOrder(() =>
        {
            fixture.Updates.DownloadAsync(Update, Arg.Any<Action<int>>(), Arg.Any<CancellationToken>());
            fixture.Updates.ApplyAndRestart(Update);
        });
        UpdateViewModel window = fixture.ShownWindows.ShouldHaveSingleItem();
        window.Message.ShouldBe("Updating to version 1.1.7…");
        window.Progress.ShouldBe(55);
    }

    [Fact]
    public async Task RunStartupUpdateAsync_Should_ContinueStartup_When_CheckThrows()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.Automatic);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new System.Net.Http.HttpRequestException("offline"));
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.RunStartupUpdateAsync();

        // Assert
        applied.ShouldBeFalse();
        fixture.Updates.DidNotReceive().ApplyAndRestart(Arg.Any<AvailableUpdate>());
    }

    [Fact]
    public async Task RunStartupUpdateAsync_Should_ContinueStartup_When_CheckTimesOut()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.Automatic);
        TaskCompletionSource<AvailableUpdate?> neverCompletes = new();
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(neverCompletes.Task);
        UpdateCoordinator coordinator = fixture.Create(checkTimeout: TimeSpan.FromMilliseconds(50));

        // Act
        bool applied = await coordinator.RunStartupUpdateAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        applied.ShouldBeFalse();
        fixture.Updates.DidNotReceive().ApplyAndRestart(Arg.Any<AvailableUpdate>());
    }

    [Fact]
    public async Task RunStartupUpdateAsync_Should_NotApplyAndCloseWindow_When_DownloadCancelled()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.Automatic);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        fixture.Updates.DownloadAsync(Update, Arg.Any<Action<int>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                fixture.ShownWindows[0].CancelCommand.Execute(null);
                call.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.RunStartupUpdateAsync();

        // Assert
        applied.ShouldBeFalse();
        fixture.Updates.DidNotReceive().ApplyAndRestart(Arg.Any<AvailableUpdate>());
        fixture.Windows.Received(1).Close(fixture.ShownWindows[0]);
    }

    [Fact]
    public async Task RunStartupUpdateAsync_Should_ContinueStartup_When_DownloadFails()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.Automatic);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        fixture.Updates.DownloadAsync(Update, Arg.Any<Action<int>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("checksum mismatch"));
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.RunStartupUpdateAsync();

        // Assert
        applied.ShouldBeFalse();
        fixture.Updates.DidNotReceive().ApplyAndRestart(Arg.Any<AvailableUpdate>());
        fixture.Windows.Received(1).Close(Arg.Any<UpdateViewModel>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_Should_ShowOneToastWithoutDownloading_When_UpdateAvailable()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.NotifyOnly);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        await coordinator.CheckAndNotifyAsync();

        // Assert
        fixture.Notifications.Received(1).NotifyWithAction(
            "Update available",
            "TSR Downloader 1.1.7 is available.",
            "Install now",
            UpdateCoordinator.InstallAction);
        await fixture.Updates.DidNotReceive().DownloadAsync(Arg.Any<AvailableUpdate>(), Arg.Any<Action<int>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(UpdateMode.Automatic)]
    [InlineData(UpdateMode.Disabled)]
    public async Task CheckAndNotifyAsync_Should_NotCheck_When_ModeIsNotNotifyOnly(UpdateMode mode)
    {
        // Arrange
        Fixture fixture = new(mode);
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        await coordinator.CheckAndNotifyAsync();

        // Assert
        await fixture.Updates.DidNotReceive().CheckAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_Should_NotNotify_When_CheckFails()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.NotifyOnly);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new System.Net.Http.HttpRequestException("rate limited"));
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        await coordinator.CheckAndNotifyAsync();

        // Assert
        fixture.Notifications.DidNotReceiveWithAnyArgs().NotifyWithAction(default!, default!, default!, default!);
        fixture.Notifications.DidNotReceiveWithAnyArgs().Notify(default!, default!);
    }

    [Theory]
    [InlineData(DownloadStatus.Queued)]
    [InlineData(DownloadStatus.Downloading)]
    [InlineData(DownloadStatus.WaitingToExtract)]
    [InlineData(DownloadStatus.Extracting)]
    public async Task InstallAsync_Should_ShowBlockedWindowWithoutDownloading_When_DownloadsActive(DownloadStatus status)
    {
        // Arrange
        Fixture fixture = new(UpdateMode.NotifyOnly);
        fixture.Downloads.Items.Returns([new DownloadItem { Status = status }]);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.InstallAsync();

        // Assert
        applied.ShouldBeFalse();
        UpdateViewModel window = fixture.ShownWindows.ShouldHaveSingleItem();
        window.IsBlocked.ShouldBeTrue();
        window.Message.ShouldBe("Downloads are in progress. Finish or cancel them first, then try again.");
        await fixture.Updates.DidNotReceive().DownloadAsync(Arg.Any<AvailableUpdate>(), Arg.Any<Action<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InstallAsync_Should_UseUpdateFromToast_When_Idle()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.NotifyOnly);
        fixture.Downloads.Items.Returns([new DownloadItem { Status = DownloadStatus.Completed }]);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        UpdateCoordinator coordinator = fixture.Create();
        await coordinator.CheckAndNotifyAsync();

        // Act
        bool applied = await coordinator.InstallAsync();

        // Assert
        applied.ShouldBeTrue();
        await fixture.Updates.Received(1).CheckAsync(Arg.Any<CancellationToken>());
        fixture.Updates.Received(1).ApplyAndRestart(Update);
    }

    [Fact]
    public async Task InstallAsync_Should_CheckFirst_When_NoUpdateRemembered()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.NotifyOnly);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        UpdateCoordinator coordinator = fixture.Create();

        // Act
        bool applied = await coordinator.InstallAsync();

        // Assert
        applied.ShouldBeTrue();
        await fixture.Updates.Received(1).CheckAsync(Arg.Any<CancellationToken>());
        fixture.Updates.Received(1).ApplyAndRestart(Update);
    }

    [Fact]
    public async Task ActionInvoked_Should_StartInstall_When_InstallActionClicked()
    {
        // Arrange
        Fixture fixture = new(UpdateMode.NotifyOnly);
        fixture.Updates.CheckAsync(Arg.Any<CancellationToken>()).Returns(Update);
        TaskCompletionSource applyCalled = new();
        fixture.Updates.When(updates => updates.ApplyAndRestart(Update)).Do(_ => applyCalled.TrySetResult());
        fixture.Create();

        // Act
        fixture.Notifications.ActionInvoked += Raise.Event<Action<string>>(UpdateCoordinator.InstallAction);

        // Assert
        await applyCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
