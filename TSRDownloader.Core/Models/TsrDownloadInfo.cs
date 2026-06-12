namespace TSRDownloader.Core.Models;

/// <summary>
/// The resolved, ready-to-fetch information for a single TSR download:
/// the (signed, CDN) URL and the file name reported by the server.
/// </summary>
/// <param name="ItemId">The TSR item id this download belongs to.</param>
/// <param name="DownloadUrl">The resolved URL the file bytes are fetched from.</param>
/// <param name="FileName">The sanitized file name to save the download as.</param>
public sealed record TsrDownloadInfo(int ItemId, string DownloadUrl, string FileName);
