namespace TSRDownloader.UI.Localization;

/// <summary>How the download list is ordered.</summary>
public enum ListSort
{
    /// <summary>In the order the links were added (the default).</summary>
    OldestFirst,
    NewestFirst,
    /// <summary>Running first, then failed, waiting and finished.</summary>
    Status,
    Name
}

/// <summary>
/// A selectable list order, shown in the sort dropdown.
/// </summary>
/// <param name="Sort">The order.</param>
/// <param name="DisplayName">Localized name shown to the user.</param>
public record SortOption(ListSort Sort, string DisplayName);
