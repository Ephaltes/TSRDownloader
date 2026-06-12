using System.Globalization;
using System.Windows;
using Shouldly;
using TSRDownloader.UI.Converters;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class ConverterTests
{
    [Fact]
    public void Convert_Should_MapBoolToVisibility_When_UsingBoolToVisibilityConverter()
    {
        // Arrange
        BoolToVisibilityConverter converter = new();

        // Act
        object whenTrue = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);
        object whenFalse = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        whenTrue.ShouldBe(Visibility.Visible);
        whenFalse.ShouldBe(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_Should_InvertBoolToVisibility_When_UsingInverseConverter()
    {
        // Arrange
        InverseBoolToVisibilityConverter converter = new();

        // Act
        object whenTrue = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);
        object whenFalse = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        whenTrue.ShouldBe(Visibility.Collapsed);
        whenFalse.ShouldBe(Visibility.Visible);
    }

    [Fact]
    public void ConvertBack_Should_Throw_When_NotSupported()
    {
        // Arrange
        BoolToVisibilityConverter converter = new();

        // Act / Assert
        Should.Throw<System.NotSupportedException>(() =>
            converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture));
    }
}
