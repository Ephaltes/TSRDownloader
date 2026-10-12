using System;
using System.Globalization;
using Shouldly;
using TSRDownloader.UI.Presentation;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class TransferFormatTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5_347_738, "5.1 MB")]
    [InlineData(3_221_225_472, "3.0 GB")]
    public void Size_Should_PickTheLargestFittingUnit_When_Formatting(long bytes, string expected)
    {
        // Act & Assert
        TransferFormat.Size(bytes, English).ShouldBe(expected);
    }

    [Fact]
    public void Size_Should_UseCultureDecimalSeparator_When_German()
    {
        // Act & Assert
        TransferFormat.Size(1536, CultureInfo.GetCultureInfo("de")).ShouldBe("1,5 KB");
    }

    [Fact]
    public void Progress_Should_ShowBothNumbersInTheTotalsUnit_When_Formatting()
    {
        // Act & Assert
        TransferFormat.Progress(512 * 1024, 4 * 1024 * 1024, English).ShouldBe("0.5 / 4.0 MB");
    }

    [Theory]
    [InlineData(42, "0:42")]
    [InlineData(725, "12:05")]
    [InlineData(3723, "1:02:03")]
    public void Duration_Should_ShowHoursOnlyWhenNeeded_When_Formatting(int seconds, string expected)
    {
        // Act & Assert
        TransferFormat.Duration(TimeSpan.FromSeconds(seconds)).ShouldBe(expected);
    }
}

public class TransferRateMeterTests
{
    [Fact]
    public void BytesPerSecond_Should_BeMeasured_When_AFullSecondHasPassed()
    {
        // Arrange
        TransferRateMeter meter = new();

        // Act
        meter.Sample(0, 0);
        meter.Sample(1000, 500);
        double? afterHalfSecond = meter.BytesPerSecond;
        meter.Sample(2000, 1000);

        // Assert
        afterHalfSecond.ShouldBeNull();
        meter.BytesPerSecond.ShouldBe(2000);
    }

    [Fact]
    public void BytesPerSecond_Should_Smooth_When_SpeedChanges()
    {
        // Arrange
        TransferRateMeter meter = new();
        meter.Sample(0, 0);
        meter.Sample(1000, 1000);

        // Act
        meter.Sample(3000, 2000);

        // Assert: moves 30 % of the way from 1000 towards 2000 B/s
        meter.BytesPerSecond.ShouldBe(1300);
    }

    [Fact]
    public void BytesPerSecond_Should_Reset_When_DownloadRestartsWithFewerBytes()
    {
        // Arrange
        TransferRateMeter meter = new();
        meter.Sample(0, 0);
        meter.Sample(5000, 1000);

        // Act
        meter.Sample(100, 1500);

        // Assert
        meter.BytesPerSecond.ShouldBeNull();
    }
}
