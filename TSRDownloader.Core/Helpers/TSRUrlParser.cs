using System.Text.RegularExpressions;

namespace TSRDownloader.Core.Helpers;

/// <summary>
/// Parses and validates The Sims Resource URLs, extracting item IDs.
/// Ported from the Python TSRUrl class.
/// </summary>
public static partial class TSRUrlParser
{
    [GeneratedRegex(@"(?<=/id/)[\d]+", RegexOptions.IgnoreCase)]
    private static partial Regex ItemIdFromIdRegex();

    [GeneratedRegex(@"(?<=/itemId/)[\d]+", RegexOptions.IgnoreCase)]
    private static partial Regex ItemIdFromItemIdRegex();

    [GeneratedRegex(@"(?<=thesimsresource\.com/downloads/)[\d]+", RegexOptions.IgnoreCase)]
    private static partial Regex ItemIdFromDownloadsRegex();

    [GeneratedRegex(@"thesimsresource\.com/", RegexOptions.IgnoreCase)]
    private static partial Regex TSRDomainRegex();

    /// <summary>
    /// Extracts the item ID from a TSR URL. Returns null if no valid ID is found.
    /// </summary>
    public static int? ExtractItemId(string url)
    {
        // TryParse: a digit run longer than int.MaxValue must not throw out of clipboard handling.
        Match m = ItemIdFromIdRegex().Match(url);
        if (m.Success) return ParseId(m.Value);

        m = ItemIdFromItemIdRegex().Match(url);
        if (m.Success) return ParseId(m.Value);

        m = ItemIdFromDownloadsRegex().Match(url);
        if (m.Success) return ParseId(m.Value);

        return null;
    }

    private static int? ParseId(string digits) => int.TryParse(digits, out int id) ? id : null;

    /// <summary>
    /// Checks whether the given URL is a valid TSR download URL.
    /// </summary>
    public static bool IsValidTSRUrl(string url)
    {
        return TSRDomainRegex().IsMatch(url) && ExtractItemId(url) is not null;
    }

    /// <summary>
    /// Extracts all valid TSR URLs from a block of text (e.g. clipboard content).
    /// Splits on any whitespace, so several links on one line or a link inside a sentence are found.
    /// </summary>
    public static List<string> ExtractTSRUrls(string text)
    {
        List<string> urls = new();
        foreach (string token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsValidTSRUrl(token))
                urls.Add(token);
        }
        return urls;
    }
}
