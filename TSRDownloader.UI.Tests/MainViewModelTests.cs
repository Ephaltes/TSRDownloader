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
    private static MainViewModel CreateViewModel(
        IDownloadService download, IClipboardService clipboard, IConfigurationService configuration,
        IUpdateService? updates = null, INotificationService? notifications = null)
    {
        return new MainViewModel(
            download,
            Substitute.For<IArchiveExtractionService>(),
            clipboard,
            configuration,
            notifications ?? Substitute.For<INotificationService>(),
            updates ?? Substitute.For<IUpdateService>(),
            LocalizationManager.Instance,
            new ImmediateDispatcher());
    }

    private static MainViewModel CreateViewModelWithUpdates(bool isInstalled, string? currentVersion = null)
    {
        IUpdateService updates = Substitute.For<IUpdateService>();
        updates.IsInstalled.Returns(isInstalled);
        updates.CurrentVersion.Returns(currentVersion);
        return CreateViewModel(Substitute.For<IDownloadService>(), Substitute.For<IClipboardService>(),
            Substitute.For<IConfigurationService>(), updates);
    }

    [Fact]
    public void VersionText_Should_ShowInstalledVersion_When_Installed()
    {
        // Arrange
        LocalizationManager.Instance.SetLanguage("en");

        // Act
        MainViewModel viewModel = CreateViewModelWithUpdates(isInstalled: true, currentVersion: "1.1.7");

        // Assert
        viewModel.VersionText.ShouldBe("Version 1.1.7");
    }

    [Fact]
    public void VersionText_Should_ShowDevelopmentBuild_When_NotInstalled()
    {
        // Arrange
        LocalizationManager.Instance.SetLanguage("en");

        // Act
        MainViewModel viewModel = CreateViewModelWithUpdates(isInstalled: false);

        // Assert
        viewModel.VersionText.ShouldBe("Development build");
    }

    [Fact]
    public void QueueStatisticsChanged_Should_CountExtractionStatesAsTerminal_When_Raised()
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
        MainViewModel viewModel = CreateViewModel(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigurationService>());

        // Act
        download.QueueStatisticsChanged += Raise.Event<System.Action>();

        // Assert
        viewModel.CompletedCount.ShouldBe(2);
        viewModel.FailedCount.ShouldBe(2);
        viewModel.OverallProgress.ShouldBe(100);
    }

    [Fact]
    public void QueueStatisticsChanged_Should_CountOnlyTheCurrentBatchAndNotifyOnce_When_QueueFinishes()
    {
        // Arrange: one finished item from earlier, then two new downloads
        IDownloadService download = Substitute.For<IDownloadService>();
        INotificationService notifications = Substitute.For<INotificationService>();
        DownloadItem history = new() { ItemId = 1, Status = DownloadStatus.Completed };
        DownloadItem first = new() { ItemId = 2, Status = DownloadStatus.Queued };
        DownloadItem second = new() { ItemId = 3, Status = DownloadStatus.Downloading, Progress = 50 };
        download.Items.Returns(new List<DownloadItem> { history, first, second });
        MainViewModel viewModel = CreateViewModel(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigurationService>(),
            notifications: notifications);
        download.QueueStatisticsChanged += Raise.Event<System.Action>();
        double progressWhileRunning = viewModel.OverallProgress;

        // Act
        first.Status = DownloadStatus.Completed;
        second.Status = DownloadStatus.Failed;
        download.QueueStatisticsChanged += Raise.Event<System.Action>();
        download.QueueStatisticsChanged += Raise.Event<System.Action>();

        // Assert
        progressWhileRunning.ShouldBe(25);
        viewModel.TotalCount.ShouldBe(2);
        viewModel.CompletedCount.ShouldBe(1);
        viewModel.FailedCount.ShouldBe(1);
        viewModel.FinishedCount.ShouldBe(2);
        viewModel.OverallProgress.ShouldBe(100);
        notifications.Received(1).Notify(Arg.Any<string>(), Arg.Any<string>(), INotificationService.StatusTag);
    }

    private static MainViewModel CreateViewModelWithFiles()
    {
        MainViewModel viewModel = CreateViewModel(Substitute.For<IDownloadService>(), Substitute.For<IClipboardService>(),
            Substitute.For<IConfigurationService>());
        viewModel.RestoreDownloads(
        [
            new() { ItemId = 11, FileName = "Bravo.zip", Url = "https://www.thesimsresource.com/id/11", Status = DownloadStatus.Completed },
            new() { ItemId = 22, FileName = "Alpha.zip", Url = "https://www.thesimsresource.com/id/22", Status = DownloadStatus.Failed },
            new() { ItemId = 33, FileName = "Charlie.package", Url = "https://www.thesimsresource.com/id/33", Status = DownloadStatus.Queued },
        ]);
        return viewModel;
    }

    private static int[] ShownIds(MainViewModel viewModel) =>
        viewModel.DownloadItemsView.Cast<DownloadItemViewModel>().Select(item => item.ItemId).ToArray();

    [Theory]
    [InlineData("alpha", new[] { 22 })]
    [InlineData(".zip", new[] { 11, 22 })]
    [InlineData("33", new[] { 33 })]
    [InlineData("  ", new[] { 11, 22, 33 })]
    public void SearchText_Should_FilterByNameLinkOrId_When_Set(string search, int[] expected)
    {
        // Arrange
        MainViewModel viewModel = CreateViewModelWithFiles();

        // Act
        viewModel.SearchText = search;

        // Assert
        ShownIds(viewModel).ShouldBe(expected);
    }

    [Theory]
    [InlineData(ListSort.OldestFirst, new[] { 11, 22, 33 })]
    [InlineData(ListSort.NewestFirst, new[] { 33, 22, 11 })]
    [InlineData(ListSort.Status, new[] { 22, 33, 11 })]
    [InlineData(ListSort.Name, new[] { 22, 11, 33 })]
    public void SelectedSort_Should_OrderTheList_When_Changed(ListSort sort, int[] expected)
    {
        // Arrange
        MainViewModel viewModel = CreateViewModelWithFiles();

        // Act
        viewModel.SelectedSort = viewModel.SortOptions.Single(option => option.Sort == sort);

        // Assert
        ShownIds(viewModel).ShouldBe(expected);
    }

    [Fact]
    public void QueueStatisticsChanged_Should_NotNotify_When_OnlyFinishedHistoryIsRestored()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        INotificationService notifications = Substitute.For<INotificationService>();
        download.Items.Returns(new List<DownloadItem> { new() { ItemId = 1, Status = DownloadStatus.Completed } });
        MainViewModel viewModel = CreateViewModel(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigurationService>(),
            notifications: notifications);

        // Act
        download.QueueStatisticsChanged += Raise.Event<System.Action>();

        // Assert
        viewModel.TotalCount.ShouldBe(1);
        notifications.DidNotReceiveWithAnyArgs().Notify(default!, default!);
    }

    [Fact]
    public void QueueStatisticsChanged_Should_NumberQueuedItemsInServiceOrder_When_Raised()
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
        MainViewModel viewModel = CreateViewModel(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigurationService>());
        viewModel.RestoreDownloads(items);

        // Act
        download.QueueStatisticsChanged += Raise.Event<System.Action>();

        // Assert
        viewModel.DownloadItems.Select(item => item.QueuePosition).ShouldBe(new[] { 0, 1, 0, 2 });
    }

    [Fact]
    public void OnClipboardContentChanged_Should_EnqueueItems_When_Monitoring()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        clipboard.ProcessClipboardContent(Arg.Any<string>())
            .Returns(new List<DownloadItem> { new() { ItemId = 1, Url = "u" } });
        IConfigurationService configuration = Substitute.For<IConfigurationService>();
        configuration.Configuration.Returns(new AppConfiguration { NotifyOnLinkFound = false });
        MainViewModel viewModel = CreateViewModel(download, clipboard, configuration);
        viewModel.StartMonitoringCommand.Execute(null);

        // Act
        viewModel.OnClipboardContentChanged("some tsr url");

        // Assert
        viewModel.DownloadItems.Count.ShouldBe(1);
        download.Received(1).EnqueueRange(Arg.Any<IEnumerable<DownloadItem>>());
    }

    [Fact]
    public void OnClipboardContentChanged_Should_Ignore_When_NotMonitoring()
    {
        // Arrange
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel viewModel = CreateViewModel(Substitute.For<IDownloadService>(), clipboard, Substitute.For<IConfigurationService>());

        // Act
        viewModel.OnClipboardContentChanged("some tsr url");

        // Assert
        clipboard.DidNotReceive().ProcessClipboardContent(Arg.Any<string>());
    }

    private static MainViewModel CreateViewModelWithItems(
        IDownloadService download, IClipboardService clipboard, params DownloadItem[] items)
    {
        clipboard.ProcessClipboardContent(Arg.Any<string>()).Returns(new List<DownloadItem>(items));
        IConfigurationService configuration = Substitute.For<IConfigurationService>();
        configuration.Configuration.Returns(new AppConfiguration { NotifyOnLinkFound = false });
        MainViewModel viewModel = CreateViewModel(download, clipboard, configuration);
        viewModel.StartMonitoringCommand.Execute(null);
        viewModel.OnClipboardContentChanged("links");
        return viewModel;
    }

    [Fact]
    public void RemoveCommand_Should_DropItemAndForgetLink_When_ServiceRemovesIt()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        download.Remove(Arg.Any<DownloadItem>()).Returns(true);
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel viewModel = CreateViewModelWithItems(download, clipboard, new DownloadItem { ItemId = 7, Status = DownloadStatus.Failed });
        viewModel.SetSelection([viewModel.DownloadItems[0]]);

        // Act
        viewModel.RemoveCommand.Execute(null);

        // Assert
        viewModel.DownloadItems.ShouldBeEmpty();
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
        MainViewModel viewModel = CreateViewModelWithItems(Substitute.For<IDownloadService>(), Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = status });

        // Act
        viewModel.SetSelection([viewModel.DownloadItems[0]]);

        // Assert
        viewModel.RemoveCommand.CanExecute(null).ShouldBe(expected);
    }

    [Fact]
    public void RetryCommand_Should_BeEnabled_When_SelectedItemFailed()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        MainViewModel viewModel = CreateViewModelWithItems(download, Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = DownloadStatus.Failed });
        viewModel.SetSelection([viewModel.DownloadItems[0]]);

        // Act
        bool canRetry = viewModel.RetryCommand.CanExecute(null);
        viewModel.RetryCommand.Execute(null);

        // Assert
        canRetry.ShouldBeTrue();
        download.Received(1).Retry(viewModel.DownloadItems[0].Model);
    }

    [Fact]
    public void RemoveCommand_Should_RemoveEverySelectedRemovableItem_When_SeveralSelected()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        download.Remove(Arg.Any<DownloadItem>()).Returns(true);
        MainViewModel viewModel = CreateViewModelWithItems(download, Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = DownloadStatus.Failed },
            new DownloadItem { ItemId = 2, Status = DownloadStatus.Extracting },
            new DownloadItem { ItemId = 3, Status = DownloadStatus.Completed },
            new DownloadItem { ItemId = 4, Status = DownloadStatus.Queued });
        viewModel.SetSelection(viewModel.DownloadItems.Take(3));

        // Act
        viewModel.RemoveCommand.Execute(null);

        // Assert
        viewModel.DownloadItems.Select(item => item.ItemId).ShouldBe(new[] { 2, 4 });
        viewModel.SelectedItems.Select(item => item.ItemId).ShouldBe(new[] { 2 });
    }

    [Fact]
    public void RetryCommand_Should_RetryOnlyFailedSelectedItems_When_SeveralSelected()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        MainViewModel viewModel = CreateViewModelWithItems(download, Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = DownloadStatus.Failed },
            new DownloadItem { ItemId = 2, Status = DownloadStatus.Completed },
            new DownloadItem { ItemId = 3, Status = DownloadStatus.ExtractionFailed });
        viewModel.SetSelection(viewModel.DownloadItems);

        // Act
        viewModel.RetryCommand.Execute(null);

        // Assert
        download.Received(1).Retry(viewModel.DownloadItems[0].Model);
        download.Received(1).Retry(viewModel.DownloadItems[2].Model);
        download.DidNotReceive().Retry(viewModel.DownloadItems[1].Model);
    }

    [Theory]
    [InlineData("https://www.thesimsresource.com/downloads/details/id/42/", "https://www.thesimsresource.com/downloads/details/id/42/")]
    [InlineData("http://thesimsresource.com/downloads/42", "http://thesimsresource.com/downloads/42")]
    [InlineData("file://C:/evil.exe?thesimsresource.com/downloads/42", "https://www.thesimsresource.com/downloads/42")]
    [InlineData("https://evil.example/thesimsresource.com/downloads/42", "https://www.thesimsresource.com/downloads/42")]
    [InlineData("thesimsresource.com/downloads/42", "https://www.thesimsresource.com/downloads/42")]
    public void TsrPageUrl_Should_OnlyPassThroughHttpTsrLinks_When_Building(string copiedUrl, string expected)
    {
        // Act
        string url = MainViewModel.TsrPageUrl(new DownloadItem { ItemId = 42, Url = copiedUrl });

        // Assert
        url.ShouldBe(expected);
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
        MainViewModel viewModel = CreateViewModel(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigurationService>());
        download.QueueStatisticsChanged += Raise.Event<System.Action>();

        // Act
        bool canExecute = viewModel.RetryFailedCommand.CanExecute(null);
        viewModel.RetryFailedCommand.Execute(null);

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
        MainViewModel viewModel = CreateViewModelWithItems(download, Substitute.For<IClipboardService>(),
            new DownloadItem { ItemId = 1, Status = DownloadStatus.Completed },
            new DownloadItem { ItemId = 2, Status = DownloadStatus.ExtractionCompleted },
            new DownloadItem { ItemId = 3, Status = DownloadStatus.Failed },
            new DownloadItem { ItemId = 4, Status = DownloadStatus.Downloading });

        // Act
        viewModel.ClearFinishedCommand.Execute(null);

        // Assert
        viewModel.DownloadItems.Select(item => item.ItemId).ShouldBe(new[] { 3, 4 });
    }

    [Fact]
    public void RestoreDownloads_Should_ListItemsMarkSeenAndHandToService_When_Called()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel viewModel = CreateViewModel(download, clipboard, Substitute.For<IConfigurationService>());
        DownloadItem[] saved =
        [
            new() { ItemId = 1, Status = DownloadStatus.Queued },
            new() { ItemId = 2, Status = DownloadStatus.Completed },
            new() { ItemId = 2, Status = DownloadStatus.Completed } // duplicate in a hand-edited file
        ];

        // Act
        viewModel.RestoreDownloads(saved);

        // Assert
        viewModel.DownloadItems.Select(item => item.ItemId).ShouldBe(new[] { 1, 2 });
        clipboard.Received(1).MarkSeen(1);
        clipboard.Received(1).MarkSeen(2);
        download.Received(1).Restore(Arg.Is<IEnumerable<DownloadItem>>(items => items != null && items.Count() == 2));
    }

    [Fact]
    public void ToggleDownloadsCommand_Should_PauseAndResumeDownloadsOnly_When_ExecutedTwice()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        MainViewModel viewModel = CreateViewModel(download, Substitute.For<IClipboardService>(), Substitute.For<IConfigurationService>());
        viewModel.StartMonitoringCommand.Execute(null);

        // Act
        viewModel.ToggleDownloadsCommand.Execute(null);
        bool pausedState = viewModel.IsDownloading;
        viewModel.ToggleDownloadsCommand.Execute(null);

        // Assert
        pausedState.ShouldBeFalse();
        viewModel.IsDownloading.ShouldBeTrue();
        viewModel.IsMonitoring.ShouldBeTrue();
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
        IConfigurationService configuration = Substitute.For<IConfigurationService>();
        configuration.Configuration.Returns(new AppConfiguration { NotifyOnLinkFound = false });
        MainViewModel viewModel = CreateViewModel(download, clipboard, configuration);
        viewModel.StartMonitoringCommand.Execute(null);
        viewModel.ToggleDownloadsCommand.Execute(null);

        // Act
        viewModel.OnClipboardContentChanged("links");

        // Assert
        viewModel.IsDownloading.ShouldBeFalse();
        viewModel.DownloadItems.Count.ShouldBe(1);
        download.Received(1).EnqueueRange(Arg.Any<IEnumerable<DownloadItem>>());
    }

    [Fact]
    public void ToggleClipboardCommand_Should_StopPickingUpLinksButKeepDownloading_When_Executed()
    {
        // Arrange
        IDownloadService download = Substitute.For<IDownloadService>();
        IClipboardService clipboard = Substitute.For<IClipboardService>();
        MainViewModel viewModel = CreateViewModel(download, clipboard, Substitute.For<IConfigurationService>());
        viewModel.StartMonitoringCommand.Execute(null);

        // Act
        viewModel.ToggleClipboardCommand.Execute(null);
        viewModel.OnClipboardContentChanged("links");

        // Assert
        viewModel.IsMonitoring.ShouldBeFalse();
        viewModel.IsDownloading.ShouldBeTrue();
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
        MainViewModel viewModel = CreateViewModel(download, clipboard, Substitute.For<IConfigurationService>());

        // Act
        int added = viewModel.AddLinks("dropped text");

        // Assert
        added.ShouldBe(1);
        viewModel.DownloadItems.Single().ItemId.ShouldBe(5);
        download.Received(1).EnqueueRange(Arg.Any<IEnumerable<DownloadItem>>());
    }
}
