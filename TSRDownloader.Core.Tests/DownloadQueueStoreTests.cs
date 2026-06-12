using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.TestHelper;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class DownloadQueueStoreTests
{
    private static DownloadQueueStore NewStore(string path) => new(path, NullLogger<DownloadQueueStore>.Instance);

    [Fact]
    public void Load_Should_ReturnSavedItems_When_RoundTripped()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("queue.json");
        DateTime added = new(2026, 10, 8, 12, 0, 0);
        DownloadItem saved = new()
        {
            ItemId = 42,
            Url = "https://www.thesimsresource.com/downloads/42",
            FileName = "mod.zip",
            FilePath = @"C:\dl\mod.zip",
            Status = DownloadStatus.ExtractionFailed,
            Progress = 100,
            ErrorMessage = "boom",
            AddedAt = added
        };

        // Act
        NewStore(path).Save([saved]);
        IReadOnlyList<DownloadItem> loaded = NewStore(path).Load();

        // Assert
        loaded.Count.ShouldBe(1);
        DownloadItem item = loaded[0];
        item.ItemId.ShouldBe(42);
        item.Url.ShouldBe(saved.Url);
        item.FileName.ShouldBe("mod.zip");
        item.FilePath.ShouldBe(@"C:\dl\mod.zip");
        item.Status.ShouldBe(DownloadStatus.ExtractionFailed);
        item.ErrorMessage.ShouldBe("boom");
        item.AddedAt.ShouldBe(added);
    }

    [Fact]
    public void Save_Should_StoreStatusByName_When_Written()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("queue.json");

        // Act
        NewStore(path).Save([new DownloadItem { ItemId = 1, Status = DownloadStatus.WaitingToExtract }]);

        // Assert
        File.ReadAllText(path).ShouldContain("\"WaitingToExtract\"");
    }

    [Fact]
    public void Load_Should_ReturnEmpty_When_FileMissing()
    {
        // Arrange
        using TempDir temp = new();

        // Act
        IReadOnlyList<DownloadItem> loaded = NewStore(temp.Combine("missing.json")).Load();

        // Assert
        loaded.ShouldBeEmpty();
    }

    [Fact]
    public void Load_Should_ReturnEmpty_When_FileIsCorrupt()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("queue.json");
        File.WriteAllText(path, "{ not json");

        // Act
        IReadOnlyList<DownloadItem> loaded = NewStore(path).Load();

        // Assert
        loaded.ShouldBeEmpty();
    }

    [Fact]
    public void Load_Should_MoveCorruptFileAside_When_FileIsUnreadable()
    {
        // Arrange
        using TempDir temp = new();
        string path = temp.Combine("queue.json");
        File.WriteAllText(path, "{ not json");
        DownloadQueueStore store = NewStore(path);

        // Act
        store.Load();
        store.Save([]);

        // Assert
        File.ReadAllText(path + ".corrupt").ShouldBe("{ not json");
    }
}
