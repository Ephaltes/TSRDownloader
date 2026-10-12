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
    private readonly Lock _fileNameLock = new();

    [GeneratedRegex(@"[\\<>/:""|?*]")]
    private static partial Regex ForbiddenFileNameCharactersRegex();

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
    public async Task<TsrDownloadInfo> ResolveDownloadAsync(int itemId, CancellationToken cancellationToken)
    {
        // 1. Get a download ticket (also sets the tsrdlticket cookie in the client's cookie container).
        string ticket = await RequestTicketAsync(itemId, cancellationToken);
        DateTimeOffset ticketRequestedAt = DateTimeOffset.UtcNow;

        // 2. Visit the ticket URL to activate the tsrdlticket cookie.
        await ActivateTicketAsync(itemId, ticket, cancellationToken);

        // 3. Wait the required delay measured from the ticket request.
        await WaitForTicketActivationAsync(ticketRequestedAt, cancellationToken);

        // 4. Resolve the actual download URL, then read the server-provided file name.
        string downloadUrl = await ResolveDownloadUrlAsync(itemId, ticket, cancellationToken);
        string fileName = await GetFileNameAsync(downloadUrl, cancellationToken);

        return new TsrDownloadInfo(itemId, downloadUrl, fileName);
    }

    /// <inheritdoc />
    public async Task<string> DownloadFileAsync(
        TsrDownloadInfo info,
        string destinationDirectory,
        Action<DownloadProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);

        // Keyed by item, so two items with the same file name never share (and corrupt) a partial file.
        string partPath = Path.Combine(destinationDirectory, $"{info.FileName}.{info.ItemId}.part");

        long startingBytes = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;

        using HttpResponseMessage response = await OpenDownloadAsync(info.DownloadUrl, startingBytes, cancellationToken);
        response.EnsureSuccessStatusCode();

        // A server that ignores the Range header answers 200 with the whole file;
        // appending that to the .part file would corrupt it.
        bool resuming = startingBytes > 0 && response.StatusCode == HttpStatusCode.PartialContent;
        if (!resuming)
            startingBytes = 0;

        long totalBytes = (response.Content.Headers.ContentLength ?? 0) + startingBytes;
        long downloadedBytes = startingBytes;
        onProgress?.Invoke(new DownloadProgress(downloadedBytes, totalBytes));

        await using (Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (FileStream fileStream = new(
            partPath, resuming ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: TransferBufferSize, useAsync: true))
        {
            byte[] buffer = new byte[TransferBufferSize];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;
                onProgress?.Invoke(new DownloadProgress(downloadedBytes, totalBytes));
            }
        }

        // Never replace an existing download: pick a free name. Locked so two downloads finishing
        // at once cannot both claim the same name.
        lock (_fileNameLock)
        {
            string filePath = FreeFilePath(destinationDirectory, info.FileName);
            File.Move(partPath, filePath);
            return filePath;
        }
    }

    /// <summary>
    /// Returns <paramref name="fileName"/> in <paramref name="directory"/>, or "name (2).ext",
    /// "name (3).ext", … for the first name that is free. For a .zip the folder it would be
    /// extracted to must be free as well, so archives never extract into each other's folder.
    /// </summary>
    private static string FreeFilePath(string directory, string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        for (int number = 1; ; number++)
        {
            string candidateStem = number == 1 ? stem : $"{stem} ({number})";
            string path = Path.Combine(directory, candidateStem + extension);
            if (!File.Exists(path) && !(ArchivePaths.IsZip(path) && Directory.Exists(ArchivePaths.ExtractionFolder(path))))
                return path;
        }
    }

    private async Task<string> RequestTicketAsync(int itemId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(TsrEndpoints.InitializeDownload(itemId), cancellationToken);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument document = JsonDocument.Parse(json);
        return GetStringProperty(document.RootElement, "ticket") is { Length: > 0 } ticket
            ? ticket
            : throw new InvalidDownloadTicketException(itemId.ToString());
    }

    private async Task ActivateTicketAsync(int itemId, string ticket, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync(TsrEndpoints.ActivateTicket(itemId, ticket), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task WaitForTicketActivationAsync(DateTimeOffset ticketRequestedAt, CancellationToken cancellationToken)
    {
        TimeSpan remaining = _ticketActivationDelay() - (DateTimeOffset.UtcNow - ticketRequestedAt);
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, cancellationToken);
    }

    private async Task<string> ResolveDownloadUrlAsync(int itemId, string ticket, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync(TsrEndpoints.ResolveDownloadUrl(itemId, ticket), cancellationToken);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument document = JsonDocument.Parse(json);

        // A missing "error" field means success; a missing "url" means we cannot continue.
        if (!string.IsNullOrEmpty(GetStringProperty(document.RootElement, "error"))
            || GetStringProperty(document.RootElement, "url") is not { Length: > 0 } url)
            throw new InvalidDownloadTicketException(itemId.ToString());

        return url;
    }

    private static string? GetStringProperty(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private async Task<string> GetFileNameAsync(string downloadUrl, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);
        using HttpResponseMessage response =
            await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

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

        string cleaned = ForbiddenFileNameCharactersRegex().Replace(fileName, "");
        cleaned = new string(cleaned.Where(character => !char.IsControl(character)).ToArray()).Trim().TrimEnd('.');

        return cleaned.Length == 0 ? FallbackFileName : cleaned;
    }

    /// <summary>
    /// Requests the file, resuming at <paramref name="startingBytes"/>. If the server rejects the
    /// range (the .part file no longer matches, e.g. it is larger than the file), starts over.
    /// </summary>
    private async Task<HttpResponseMessage> OpenDownloadAsync(
        string downloadUrl, long startingBytes, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await SendDownloadRequestAsync(downloadUrl, startingBytes, cancellationToken);
        if (startingBytes == 0 || response.StatusCode != HttpStatusCode.RequestedRangeNotSatisfiable)
            return response;

        response.Dispose();
        return await SendDownloadRequestAsync(downloadUrl, 0, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendDownloadRequestAsync(
        string downloadUrl, long startingBytes, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);

        if (startingBytes > 0)
            request.Headers.Range = new RangeHeaderValue(startingBytes, null);

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public void Dispose() => _httpClient.Dispose();
}
