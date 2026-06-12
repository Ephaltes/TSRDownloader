using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.UI.Presentation;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class DownloadStatusPresenterTests
{
    [Theory]
    [InlineData(DownloadStatus.Queued, "Status_Queued")]
    [InlineData(DownloadStatus.Downloading, "Status_Downloading")]
    [InlineData(DownloadStatus.Completed, "Status_Completed")]
    [InlineData(DownloadStatus.Failed, "Status_Failed")]
    [InlineData(DownloadStatus.WaitingToExtract, "Status_WaitingToExtract")]
    [InlineData(DownloadStatus.Extracting, "Status_Extracting")]
    [InlineData(DownloadStatus.ExtractionCompleted, "Status_ExtractionCompleted")]
    [InlineData(DownloadStatus.ExtractionFailed, "Status_ExtractionFailed")]
    public void For_Should_ReturnLabelKey_When_GivenStatus(DownloadStatus status, string expectedKey)
    {
        // Act
        string labelKey = DownloadStatusPresenter.For(status).LabelKey;

        // Assert
        labelKey.ShouldBe(expectedKey);
    }

    [Fact]
    public void For_Should_SetIndeterminateFlags_When_StatusIsExtractingOrWaiting()
    {
        // Act
        bool extractingIndeterminate = DownloadStatusPresenter.For(DownloadStatus.Extracting).IsIndeterminate;
        bool waitingIndeterminate = DownloadStatusPresenter.For(DownloadStatus.WaitingToExtract).IsIndeterminate;

        // Assert
        extractingIndeterminate.ShouldBeFalse();
        waitingIndeterminate.ShouldBeTrue();
    }

    [Fact]
    public void For_Should_ReturnFallback_When_StatusIsUnknown()
    {
        // Act
        string labelKey = DownloadStatusPresenter.For((DownloadStatus)999).LabelKey;

        // Assert
        labelKey.ShouldBe("Status_Unknown");
    }
}
