namespace TSRDownloader.Core.Helpers;

/// <summary>
/// Central definition of every The Sims Resource HTTP endpoint used by the app.
/// Keeping these in one place means a URL or query-parameter change on TSR's side
/// only has to be fixed here, not hunted down across the service classes.
/// </summary>
internal static class TsrEndpoints
{
    public const string BaseUrl = "https://www.thesimsresource.com";

    /// <summary>Requests a download ticket and sets the <c>tsrdlticket</c> cookie.</summary>
    public static string InitDownload(int itemId) =>
        $"{BaseUrl}/ajax.php?c=downloads&a=initDownload&itemid={itemId}&format=zip";

    /// <summary>Visited to activate the <c>tsrdlticket</c> cookie for the item.</summary>
    public static string ActivateTicket(int itemId, string ticket) =>
        $"{BaseUrl}/downloads/download/itemId/{itemId}/ticket/{ticket}";

    /// <summary>Resolves the actual (CDN) download URL for an activated ticket.</summary>
    public static string ResolveDownloadUrl(int itemId, string ticket) =>
        $"{BaseUrl}/ajax.php?c=downloads&a=getdownloadurl&ajax=1&itemid={itemId}&mid=0&lk=0&ticket={ticket}";
}
