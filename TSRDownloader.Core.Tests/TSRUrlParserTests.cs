using System.Collections.Generic;
using Shouldly;
using TSRDownloader.Core.Helpers;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class TSRUrlParserTests
{
    [Theory]
    [InlineData("https://www.thesimsresource.com/downloads/details/id/1646133", 1646133)]
    [InlineData("https://www.thesimsresource.com/downloads/download/itemId/1646133", 1646133)]
    [InlineData("https://www.thesimsresource.com/downloads/1646133", 1646133)]
    public void ExtractItemId_Should_ReturnId_When_UrlMatchesKnownShape(string url, int expected)
    {
        // Arrange
        // (url and expected are supplied by the theory data)

        // Act
        int? itemId = TSRUrlParser.ExtractItemId(url);

        // Assert
        itemId.ShouldBe(expected);
    }

    [Theory]
    [InlineData("https://example.com/no-id-here")]
    [InlineData("not a url at all")]
    [InlineData("")]
    public void ExtractItemId_Should_ReturnNull_When_UrlIsUnrecognized(string url)
    {
        // Act
        int? itemId = TSRUrlParser.ExtractItemId(url);

        // Assert
        itemId.ShouldBeNull();
    }

    [Theory]
    [InlineData("https://www.thesimsresource.com/downloads/1646133", true)]
    [InlineData("https://www.thesimsresource.com/about", false)]
    [InlineData("https://example.com/downloads/1646133", false)]
    public void IsValidTSRUrl_Should_ReturnExpected_When_GivenUrl(string url, bool expected)
    {
        // Act
        bool isValid = TSRUrlParser.IsValidTSRUrl(url);

        // Assert
        isValid.ShouldBe(expected);
    }

    [Fact]
    public void ExtractTSRUrls_Should_ReturnOnlyValidLines_When_TextContainsMixedContent()
    {
        // Arrange
        string text = string.Join('\n', new[]
        {
            "https://www.thesimsresource.com/downloads/1",
            "garbage line",
            "https://www.thesimsresource.com/downloads/details/id/2"
        });

        // Act
        List<string> urls = TSRUrlParser.ExtractTSRUrls(text);

        // Assert
        urls.Count.ShouldBe(2);
    }

    [Fact]
    public void ExtractItemId_Should_ReturnNull_When_IdOverflowsInt()
    {
        // Act
        int? id = TSRUrlParser.ExtractItemId("https://www.thesimsresource.com/downloads/99999999999999999999");

        // Assert
        id.ShouldBeNull();
    }

    [Fact]
    public void ExtractTSRUrls_Should_ReturnEachUrl_When_UrlsShareALineWithText()
    {
        // Arrange
        string text = "look: https://www.thesimsresource.com/downloads/1 and "
                      + "https://www.thesimsresource.com/downloads/details/id/2\tok";

        // Act
        List<string> urls = TSRUrlParser.ExtractTSRUrls(text);

        // Assert
        urls.ShouldBe(new[]
        {
            "https://www.thesimsresource.com/downloads/1",
            "https://www.thesimsresource.com/downloads/details/id/2"
        });
    }
}
