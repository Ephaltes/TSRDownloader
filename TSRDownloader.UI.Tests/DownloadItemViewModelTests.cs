using System;
using System.Globalization;
using System.Threading;
using NSubstitute;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;
using TSRDownloader.UI.ViewModels;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class DownloadItemViewModelTests
{
    private static DownloadItemViewModel VmFor(DownloadItem model)
    {
        LocalizationManager.Instance.SetLanguage("en");
        return new DownloadItemViewModel(model, Substitute.For<INotificationService>());
    }

    // ProgressText interpolates Progress with "F1", which is sensitive to the thread's
    // CurrentCulture (number formatting), not LocalizationManager's language. Force an
    // invariant culture for these assertions so the test is deterministic regardless of
    // the OS locale it runs under.
    private static void WithInvariantCulture(Action action)
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        try { action(); }
        finally { Thread.CurrentThread.CurrentCulture = original; }
    }

    [Fact]
    public void ProgressText_Should_ShowPercentage_When_Downloading()
    {
        WithInvariantCulture(() =>
        {
            // Arrange
            DownloadItem model = new() { Status = DownloadStatus.Downloading, Progress = 42.5 };

            // Act
            string progressText = VmFor(model).ProgressText;

            // Assert
            progressText.ShouldBe("42.5%");
        });
    }

    [Fact]
    public void ProgressText_Should_ShowPercentageAndExtractingLabel_When_Extracting()
    {
        WithInvariantCulture(() =>
        {
            // Arrange
            DownloadItem model = new() { Status = DownloadStatus.Extracting, Progress = 10 };

            // Act
            DownloadItemViewModel vm = VmFor(model);

            // Assert
            vm.ProgressText.ShouldBe("10.0%");
            vm.StatusText.ShouldBe("Extracting...");
        });
    }

    [Fact]
    public void Progress_Should_BeFull_When_ExtractionCompleted()
    {
        // Arrange
        DownloadItem model = new() { Status = DownloadStatus.ExtractionCompleted, Progress = 3 };

        // Act
        double progress = VmFor(model).Progress;

        // Assert
        progress.ShouldBe(100);
    }

    [Fact]
    public void ProgressText_Should_ShowErrorMessage_When_ExtractionFailed()
    {
        // Arrange
        DownloadItem model = new() { Status = DownloadStatus.ExtractionFailed, ErrorMessage = "kaputt" };

        // Act
        string progressText = VmFor(model).ProgressText;

        // Assert
        progressText.ShouldBe("kaputt");
    }
}
