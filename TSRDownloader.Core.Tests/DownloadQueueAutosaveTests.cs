using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class DownloadQueueAutosaveTests
{
    private readonly IDownloadService _downloads = Substitute.For<IDownloadService>();
    private readonly IArchiveExtractionService _extraction = Substitute.For<IArchiveExtractionService>();
    private readonly IDownloadQueueStore _store = Substitute.For<IDownloadQueueStore>();
    private readonly List<DownloadItem> _items = [new() { ItemId = 1 }];

    public DownloadQueueAutosaveTests() => _downloads.Items.Returns(_items);

    [Fact]
    public async Task QueueStatsChanged_Should_SaveOnceAfterDelay_When_RaisedRepeatedly()
    {
        // Arrange
        TaskCompletionSource saved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.When(store => store.Save(Arg.Any<IReadOnlyList<DownloadItem>>())).Do(_ => saved.TrySetResult());
        using DownloadQueueAutosave autosave = new(_downloads, _extraction, _store, TimeSpan.FromMilliseconds(50));

        // Act
        _downloads.QueueStatsChanged += Raise.Event<Action>();
        _downloads.QueueStatsChanged += Raise.Event<Action>();
        _extraction.StatsChanged += Raise.Event<Action>();
        await saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(150);

        // Assert
        _store.Received(1).Save(_items);
    }

    [Fact]
    public void Dispose_Should_SavePendingChanges_When_DelayHasNotElapsed()
    {
        // Arrange
        DownloadQueueAutosave autosave = new(_downloads, _extraction, _store, TimeSpan.FromHours(1));
        _downloads.QueueStatsChanged += Raise.Event<Action>();

        // Act
        autosave.Dispose();

        // Assert
        _store.Received(1).Save(_items);
    }

    [Fact]
    public void Dispose_Should_NotSave_When_NothingChanged()
    {
        // Arrange
        DownloadQueueAutosave autosave = new(_downloads, _extraction, _store, TimeSpan.FromHours(1));

        // Act
        autosave.Dispose();

        // Assert
        _store.DidNotReceive().Save(Arg.Any<IReadOnlyList<DownloadItem>>());
    }
}
