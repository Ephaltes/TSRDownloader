using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using TSRDownloader.Core.Exceptions;
using TSRDownloader.Core.Helpers;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Implements the The Sims Resource download protocol over HTTP.
/// Ported from the Python <c>TSRDownload</c> reference implementation.
/// </summary>
public partial class TsrDownloadClient : ITsrDownloadClient, IDisposable
{
    private const int TransferBufferSize = 128 * 1024;
    private const string FallbackFileName = "download.zip";
    private static readonly TimeSpan DefaultTicketActivationDelay = TimeSpan.FromSeconds(8);

    private readonly HttpClient _httpClient;
    private readonly Func<TimeSpan> _ticketActivationDelay;

    [GeneratedRegex(@"[\\<>/:""|?*]")]
    private static partial Regex ForbiddenFileNameCharsRegex();

    /// <param name="httpClient">
    /// HTTP client with cookies enabled — the download ticket is carried as a cookie between requests.
    /// </param>
    /// <param name="ticketActivationDelay">
    /// Returns how long TSR needs between requesting a ticket and resolving the download URL.
    /// Read for every download, so a changed setting applies immediately. Defaults to 8 seconds.
    /// </param>
    public TsrDownloadClient(HttpClient httpClient, Func<TimeSpan>? ticketActivationDelay = null)
    {
        _httpClient = httpClient;
        _ticketActivationDelay = ticketActivationDelay ?? (() => DefaultTicketActivationDelay);
    }

    /// <inheritdoc />
    public async Task<TsrDownloadInfo> ResolveDownloadAsync(int itemId, CancellationToken ct)
    {
        // 1. Get a download ticket (also sets the tsrdlticket cookie in the client's cookie container).
        string ticket = await RequestTicketAsync(itemId, ct);
        long ticketInitTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 2. Visit the ticket URL to activate the tsrdlticket cookie.
        await ActivateTicketAsync(itemId, ticket, ct);

        // 3. Wait the required delay measured from ticket initialization.
        await WaitForTicketActivationAsync(ticketInitTimeMs, ct);

        // 4. Resolve the actual download URL, then read the server-provided file name.
        string downloadUrl = await ResolveDownloadUrlAsync(itemId, ticket, ct);
        string fileName = await GetFileNameAsync(downloadUrl, ct);

        return new TsrDownloadInfo(itemId, downloadUrl, fileName);
    }

    /// <inheritdoc />
    public async Task<string> DownloadFileAsync(
        TsrDownloadInfo info,
        string destinationDirectory,
        Action<DownloadProgress>? onProgress,
        CancellationToken ct)
    {
        Directory.CreateDirectory(destinationDirectory);

        string filePath = Path.Combine(destinationDirectory, info.FileName);
        string partPath = filePath + ".part";

        long startingBytes = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;

        using HttpResponseMessage response = await OpenDownloadAsync(info.DownloadUrl, startingBytes, ct);
        response.EnsureSuccessStatusCode();

        // A server that ignores the Range header answers 200 with the whole file;
        // appending that to the .part file would corrupt it.
        bool resuming = startingBytes > 0 && response.StatusCode == HttpStatusCode.PartialContent;
        if (!resuming)
            startingBytes = 0;

        long totalBytes = (response.Content.Headers.ContentLength ?? 0) + startingBytes;
        long downloadedBytes = startingBytes;
        onProgress?.Invoke(new DownloadProgress(downloadedBytes, totalBytes));

        await using (Stream stream = await response.Content.ReadAsStreamAsync(ct))
        await using (FileStream fileStream = new(
            partPath, resuming ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: TransferBufferSize, useAsync: true))
        {
            byte[] buffer = new byte[TransferBufferSize];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                downloadedBytes += bytesRead;
                onProgress?.Invoke(new DownloadProgress(downloadedBytes, totalBytes));
            }
        }

        // Replace any existing file with the freshly completed .part file.
        File.Move(partPath, filePath, overwrite: true);

        return filePath;
    }

    private async Task<string> RequestTicketAsync(int itemId, CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(TsrEndpoints.InitDownload(itemId), ct);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(ct);
        using JsonDocument doc = JsonDocument.Parse(json);
        return GetStringProperty(doc.RootElement, "ticket") is { Length: > 0 } ticket
            ? ticket
            : throw new InvalidDownloadTicketException(itemId.ToString());
    }

    private async Task ActivateTicketAsync(int itemId, string ticket, CancellationToken ct)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync(TsrEndpoints.ActivateTicket(itemId, ticket), ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task WaitForTicketActivationAsync(long ticketInitTimeMs, CancellationToken ct)
    {
        long elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ticketInitTimeMs;
        long remainingMs = (long)_ticketActivationDelay().TotalMilliseconds - elapsedMs;
        if (remainingMs > 0)
            await Task.Delay((int)remainingMs, ct);
    }

    private async Task<string> ResolveDownloadUrlAsync(int itemId, string ticket, CancellationToken ct)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync(TsrEndpoints.ResolveDownloadUrl(itemId, ticket), ct);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(ct);
        using JsonDocument doc = JsonDocument.Parse(json);

        // A missing "error" field means success; a missing "url" means we cannot continue.
        if (!string.IsNullOrEmpty(GetStringProperty(doc.RootElement, "error"))
            || GetStringProperty(doc.RootElement, "url") is not { Length: > 0 } url)
            throw new InvalidDownloadTicketException(itemId.ToString());

        return url;
    }

    private static string? GetStringProperty(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private async Task<string> GetFileNameAsync(string downloadUrl, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);
        using HttpResponseMessage response =
            await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        ContentDispositionHeaderValue? disposition = response.Content.Headers.ContentDisposition;
        return SanitizeFileName(disposition?.FileNameStar ?? disposition?.FileName);
    }

    /// <summary>
    /// Turns the server-supplied name into a safe single file name inside the download directory:
    /// strips path separators, reserved and control characters, and trailing dots/spaces
    /// (which Windows drops silently). Falls back to <see cref="FallbackFileName"/> if nothing usable remains.
    /// </summary>
    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return FallbackFileName;

        string cleaned = ForbiddenFileNameCharsRegex().Replace(fileName, "");
        cleaned = new string(cleaned.Where(c => !char.IsControl(c)).ToArray()).Trim().TrimEnd('.');

        return cleaned.Length == 0 ? FallbackFileName : cleaned;
    }

    /// <summary>
    /// Requests the file, resuming at <paramref name="startingBytes"/>. If the server rejects the
    /// range (the .part file no longer matches, e.g. it is larger than the file), starts over.
    /// </summary>
    private async Task<HttpResponseMessage> OpenDownloadAsync(
        string downloadUrl, long startingBytes, CancellationToken ct)
    {
        HttpResponseMessage response = await SendDownloadRequestAsync(downloadUrl, startingBytes, ct);
        if (startingBytes == 0 || response.StatusCode != HttpStatusCode.RequestedRangeNotSatisfiable)
            return response;

        response.Dispose();
        return await SendDownloadRequestAsync(downloadUrl, 0, ct);
    }

    private async Task<HttpResponseMessage> SendDownloadRequestAsync(
        string downloadUrl, long startingBytes, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);

        if (startingBytes > 0)
            request.Headers.Range = new RangeHeaderValue(startingBytes, null);

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public void Dispose() => _httpClient.Dispose();
}
