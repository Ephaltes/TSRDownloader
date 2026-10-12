using System.Linq;
using System.Collections.Generic;
using NSubstitute;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;
using TSRDownloader.UI.ViewModels;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class MainViewModelTests
{
    private static MainViewModel NewVm(
        IDownloadService download, IClipboardService clipboard, IConfigService config,
        IUpdateService? updates = null)
    {
        return new MainViewModel(
            download,
            Substitute.For<IArchiveExtractionService>(),
            clipboard,
            config,
            Substitute.For<INotificationService>(),
            updates ?? Substitute.For<IUpdateService>(),
            LocalizationManager.Instance,
            new ImmediateDispatcher());
    }

    private static MainViewModel NewVmWithUpdates(bool isInstalled, string? currentVersion = null)
    {
        IUpdateService updates = Substitute.For<IUpdateService>();
        updates.IsInstalled.Returns(isInstalled);
        updates.CurrentVersion.Returns(currentVersion);
        return NewVm(Substitute.For<IDownloadService>(), Substitute.For<IClipboardService>(),
            Substitute.For<IConfigService>(), updates);
    }

    [Fact]
    public void VersionText_Should_ShowInstalledVersion_When_Installed()
    {
        // Arrange
        LocalizationManager.Instance.SetLanguage("en");

        // Act
        MainViewModel vm = NewVmWithUpdates(isInstalled: true, currentVersion: "1.1.7");

        // Assert
        vm.VersionText.ShouldBe("Version 1.1.7");
    }

    [Fact]
    public void VersionText_Should_ShowDevelopmentBuild_When_NotInstalled()
    {
        // Arrange
        LocalizationManager.Instance.SetLanguage("en");

        // Act
        MainViewModel vm = NewVmWithUpdates(isInstalled: false);

        // Assert
        vm.VersionText.ShouldBe("Development build");
    }

    [Fact]
    public void QueueStatsChanged_Should_CountExtractionStatesAsTerminal_When_Raised()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        download.Items.Returns(new List<DownloadItem>
        {
            new() { Status = DownloadStatus.Completed },
            new() { Status = DownloadStatus.ExtractionCompleted },
            new() { Status = DownloadStatus.Failed },
            new() { Status = DownloadStatus.ExtractionFailed },
        });
        MainViewModel vm = NewVm(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigService>());

        // Act
        download.QueueStatsChanged += Raise.Event<System.Action>();

        // Assert
        vm.CompletedCount.ShouldBe(2);
        vm.FailedCount.ShouldBe(2);
        vm.OverallProgress.ShouldBe(100);
    }

    [Fact]
    public void QueueStatsChanged_Should_NumberQueuedItemsInServiceOrder_When_Raised()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        DownloadItem[] items =
        [
            new() { ItemId = 1, Status = DownloadStatus.Downloading },
            new() { ItemId = 2, Status = DownloadStatus.Queued },
            new() { ItemId = 3, Status = DownloadStatus.Completed },
            new() { ItemId = 4, Status = DownloadStatus.Queued },
        ];
        download.Items.Returns(items);
        MainViewModel vm = NewVm(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigService>());
        vm.RestoreDownloads(items);

        // Act
        download.QueueStatsChanged += Raise.Event<System.Action>();

        // Assert
        vm.DownloadItems.Select(item => item.QueuePosition).ShouldBe(new[] { 0, 1, 0, 2 });
    }

    [Fact]
    public void OnClipboardContentChanged_Should_EnqueueItems_When_Monitoring()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        clipboard.ProcessClipboardContent(Arg.Any<string>())
            .Returns(new List<DownloadItem> { new() { ItemId = 1, Url = "u" } });
        IConfigService config = Substitute.For<IConfigService>();
        config.Config.Returns(new AppConfig { NotifyOnLinkFound = false });
        MainViewModel vm = NewVm(download, clipboard, config);
        vm.StartMonitoringCommand.Execute(null);

        // Act
        vm.OnClipboardContentChanged("some tsr url");

        // Assert
        vm.DownloadItems.Count.ShouldBe(1);
        download.Received(1).EnqueueRange(Arg.Any<IEnumerable<DownloadItem>>());
    }

    [Fact]
    public void OnClipboardContentChanged_Should_Ignore_When_NotMonitoring()
    {
        // Arrange
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel vm = NewVm(Substitute.For<IDownloadService>(), clipboard, Substitute.For<IConfigService>());

        // Act
        vm.OnClipboardContentChanged("some tsr url");

        // Assert
        clipboard.DidNotReceive().ProcessClipboardContent(Arg.Any<string>());
    }

    private static MainViewModel VmWithItems(
        IDownloadService download, IClipboardService clipboard, params DownloadItem[] items)
    {
        clipboard.ProcessClipboardContent(Arg.Any<string>()).Returns(new List<DownloadItem>(items));
        IConfigService config = Substitute.For<IConfigService>();
        config.Config.Returns(new AppConfig { NotifyOnLinkFound = false });
        MainViewModel vm = NewVm(download, clipboard, config);
        vm.StartMonitoringCommand.Execute(null);
        vm.OnClipboardContentChanged("links");
        return vm;
    }

    [Fact]
    public void RemoveCommand_Should_DropItemAndForgetLink_When_ServiceRemovesIt()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        download.Remove(Arg.Any<DownloadItem>()).Returns(true);
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel vm = VmWithItems(download, clipboard, new DownloadItem { ItemId = 7, Status = DownloadStatus.Failed });
        vm.SelectedItem = vm.DownloadItems[0];

        // Act
        vm.RemoveCommand.Execute(null);

        // Assert
        vm.DownloadItems.ShouldBeEmpty();
        clipboard.Received(1).Forget(7);
    }

    [Theory]
    [InlineData(DownloadStatus.WaitingToExtract, false)]
    [InlineData(DownloadStatus.Extracting, false)]
    [InlineData(DownloadStatus.Failed, true)]
    [InlineData(DownloadStatus.Downloading, true)]
    public void RemoveCommand_Should_BeEnabledOnlyOutsideExtraction_When_ItemSelected(DownloadStatus status, bool expected)
    {
        // Arrange
        MainViewModel vm = VmWithItems(Substitute.For<IDownloadService>(), Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = status });

        // Act
        vm.SelectedItem = vm.DownloadItems[0];

        // Assert
        vm.RemoveCommand.CanExecute(null).ShouldBe(expected);
    }

    [Fact]
    public void RetryCommand_Should_BeEnabled_When_SelectedItemFailed()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        MainViewModel vm = VmWithItems(download, Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = DownloadStatus.Failed });
        vm.SelectedItem = vm.DownloadItems[0];

        // Act
        bool canRetry = vm.RetryCommand.CanExecute(null);
        vm.RetryCommand.Execute(null);

        // Assert
        canRetry.ShouldBeTrue();
        download.Received(1).Retry(vm.DownloadItems[0].Model);
    }

    [Fact]
    public void RetryFailedCommand_Should_RetryEveryFailedItem_When_Executed()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        DownloadItem failed = new() { ItemId = 1, Status = DownloadStatus.Failed };
        DownloadItem extractionFailed = new() { ItemId = 2, Status = DownloadStatus.ExtractionFailed };
        DownloadItem completed = new() { ItemId = 3, Status = DownloadStatus.Completed };
        download.Items.Returns(new List<DownloadItem> { failed, extractionFailed, completed });
        MainViewModel vm = NewVm(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigService>());
        download.QueueStatsChanged += Raise.Event<System.Action>();

        // Act
        bool canExecute = vm.RetryFailedCommand.CanExecute(null);
        vm.RetryFailedCommand.Execute(null);

        // Assert
        canExecute.ShouldBeTrue();
        download.Received(1).Retry(failed);
        download.Received(1).Retry(extractionFailed);
        download.DidNotReceive().Retry(completed);
    }

    [Fact]
    public void ClearFinishedCommand_Should_RemoveOnlyFinishedItems_When_Executed()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        download.Remove(Arg.Any<DownloadItem>()).Returns(true);
        MainViewModel vm = VmWithItems(download, Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = DownloadStatus.Completed },
            new DownloadItem { ItemId = 2, Status = DownloadStatus.ExtractionCompleted },
            new DownloadItem { ItemId = 3, Status = DownloadStatus.Failed },
            new DownloadItem { ItemId = 4, Status = DownloadStatus.Downloading });

        // Act
        vm.ClearFinishedCommand.Execute(null);

        // Assert
        vm.DownloadItems.Select(item => item.ItemId).ShouldBe(new[] { 3, 4 });
    }

    [Fact]
    public void RestoreDownloads_Should_ListItemsMarkSeenAndHandToService_When_Called()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel vm = NewVm(download, clipboard, Substitute.For<IConfigService>());
        DownloadItem[] saved =
        [
            new() { ItemId = 1, Status = DownloadStatus.Queued },
            new() { ItemId = 2, Status = DownloadStatus.Completed },
            new() { ItemId = 2, Status = DownloadStatus.Completed } // duplicate in a hand-edited file
        ];

        // Act
        vm.RestoreDownloads(saved);

        // Assert
        vm.DownloadItems.Select(item => item.ItemId).ShouldBe(new[] { 1, 2 });
        clipboard.Received(1).MarkSeen(1);
        clipboard.Received(1).MarkSeen(2);
        download.Received(1).Restore(Arg.Is<IEnumerable<DownloadItem>>(items => items != null && items.Count() == 2));
    }

    [Fact]
    public void ToggleDownloadsCommand_Should_PauseAndResumeDownloadsOnly_When_ExecutedTwice()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        MainViewModel vm = NewVm(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigService>());
        vm.StartMonitoringCommand.Execute(null);

        // Act
        vm.ToggleDownloadsCommand.Execute(null);
        bool pausedState = vm.IsDownloading;
        vm.ToggleDownloadsCommand.Execute(null);

        // Assert
        pausedState.ShouldBeFalse();
        vm.IsDownloading.ShouldBeTrue();
        vm.IsMonitoring.ShouldBeTrue();
        download.Received(1).Stop();
        download.Received(2).Start();
    }

    [Fact]
    public void OnClipboardContentChanged_Should_StillQueueLinks_When_DownloadsPaused()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        clipboard.ProcessClipboardContent(Arg.Any<string>())
            .Returns(new List<DownloadItem> { new() { ItemId = 9, Url = "u" } });
        IConfigService config = Substitute.For<IConfigService>();
        config.Config.Returns(new AppConfig { NotifyOnLinkFound = false });
        MainViewModel vm = NewVm(download, clipboard, config);
        vm.StartMonitoringCommand.Execute(null);
        vm.ToggleDownloadsCommand.Execute(null);

        // Act
        vm.OnClipboardContentChanged("links");

        // Assert
        vm.IsDownloading.ShouldBeFalse();
        vm.DownloadItems.Count.ShouldBe(1);
        download.Received(1).EnqueueRange(Arg.Any<IEnumerable<DownloadItem>>());
    }

    [Fact]
    public void ToggleClipboardCommand_Should_StopPickingUpLinksButKeepDownloading_When_Executed()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel vm = NewVm(download, clipboard, Substitute.For<IConfigService>());
        vm.StartMonitoringCommand.Execute(null);

        // Act
        vm.ToggleClipboardCommand.Execute(null);
        vm.OnClipboardContentChanged("links");

        // Assert
        vm.IsMonitoring.ShouldBeFalse();
        vm.IsDownloading.ShouldBeTrue();
        clipboard.DidNotReceive().ProcessClipboardContent(Arg.Any<string>());
        download.DidNotReceive().Stop();
    }

    [Fact]
    public void AddLinks_Should_QueueLinks_When_ClipboardWatchingIsPaused()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        clipboard.ProcessClipboardContent("dropped text")
            .Returns(new List<DownloadItem> { new() { ItemId = 5, Url = "u" } });
        MainViewModel vm = NewVm(download, clipboard, Substitute.For<IConfigService>());

        // Act
        int added = vm.AddLinks("dropped text");

        // Assert
        added.ShouldBe(1);
        vm.DownloadItems.Single().ItemId.ShouldBe(5);
        download.Received(1).EnqueueRange(Arg.Any<IEnumerable<DownloadItem>>());
    }
}
