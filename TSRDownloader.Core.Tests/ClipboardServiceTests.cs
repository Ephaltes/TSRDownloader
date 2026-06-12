using System.Collections.Generic;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class ClipboardServiceTests
{
    [Fact]
    public void ProcessClipboardContent_Should_ParseItems_When_TextContainsValidUrl()
    {
        // Arrange
        ClipboardService service = new();

        // Act
        List<DownloadItem> items = service.ProcessClipboardContent(
            "https://www.thesimsresource.com/downloads/details/id/42");

        // Assert
        items.Count.ShouldBe(1);
        items[0].ItemId.ShouldBe(42);
        items[0].Status.ShouldBe(DownloadStatus.Queued);
    }

    [Fact]
    public void ProcessClipboardContent_Should_ReturnEmpty_When_UrlAlreadySeen()
    {
        // Arrange
        ClipboardService service = new();
        const string url = "https://www.thesimsresource.com/downloads/details/id/42";
        service.ProcessClipboardContent(url);

        // Act
        List<DownloadItem> secondCall = service.ProcessClipboardContent(url);

        // Assert
        secondCall.ShouldBeEmpty();
    }

    [Fact]
    public void ProcessClipboardContent_Should_ReturnItemAgain_When_ItemWasForgotten()
    {
        // Arrange
        ClipboardService service = new();
        const string url = "https://www.thesimsresource.com/downloads/details/id/42";
        service.ProcessClipboardContent(url);

        // Act
        service.Forget(42);
        List<DownloadItem> secondCall = service.ProcessClipboardContent(url);

        // Assert
        secondCall.Count.ShouldBe(1);
    }

    [Fact]
    public void ProcessClipboardContent_Should_ReturnEmpty_When_TextHasNoUrl()
    {
        // Arrange
        ClipboardService service = new();

        // Act
        List<DownloadItem> items = service.ProcessClipboardContent("nothing to see here");

        // Assert
        items.ShouldBeEmpty();
    }
}
