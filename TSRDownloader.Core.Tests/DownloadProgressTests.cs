using Shouldly;
using TSRDownloader.Core.Models;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class DownloadProgressTests
{
    [Fact]
    public void Percentage_Should_ReturnZero_When_TotalIsZero()
    {
        // Arrange
        DownloadProgress progress = new(100, 0);

        // Act
        double percentage = progress.Percentage;

        // Assert
        percentage.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, 3, 33.3)]
    [InlineData(50, 200, 25)]
    public void Percentage_Should_ComputeRoundedValue_When_TotalIsPositive(long downloaded, long total, double expected)
    {
        // Arrange
        DownloadProgress progress = new(downloaded, total);

        // Act
        double percentage = progress.Percentage;

        // Assert
        percentage.ShouldBe(expected);
    }
}
