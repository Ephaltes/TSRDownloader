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
        IDownloadService download, IClipboardService clipboard, IConfigService config)
    {
        return new MainViewModel(
            download,
            Substitute.For<IArchiveExtractionService>(),
            clipboard,
            config,
            Substitute.For<INotificationService>(),
            LocalizationManager.Instance,
            new ImmediateDispatcher());
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
    public void ToggleMonitoringCommand_Should_PauseAndResume_When_ExecutedTwice()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        MainViewModel vm = NewVm(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigService>());
        vm.StartMonitoringCommand.Execute(null);

        // Act
        vm.ToggleMonitoringCommand.Execute(null);
        bool pausedState = vm.IsMonitoring;
        vm.ToggleMonitoringCommand.Execute(null);

        // Assert
        pausedState.ShouldBeFalse();
        vm.IsMonitoring.ShouldBeTrue();
        download.Received(1).Stop();
    }
}
