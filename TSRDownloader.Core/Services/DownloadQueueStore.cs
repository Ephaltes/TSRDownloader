using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TSRDownloader.Core.Helpers;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Stores the download list as JSON (<c>queue.json</c> in the data directory).
/// A missing or unreadable file yields an empty list; failures are logged, never thrown,
/// so a broken queue file can never prevent the app from starting. An unreadable file is
/// renamed to <c>queue.json.corrupt</c> so it is not lost when the next save happens.
/// </summary>
public sealed class DownloadQueueStore : IDownloadQueueStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly ILogger<DownloadQueueStore> _logger;

    public DownloadQueueStore(string filePath, ILogger<DownloadQueueStore> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    public IReadOnlyList<DownloadItem> Load()
    {
        if (!File.Exists(_filePath))
            return [];

        try
        {
            QueueFile? file = JsonSerializer.Deserialize<QueueFile>(File.ReadAllText(_filePath), SerializerOptions);
            List<DownloadItem> items = file?.Items ?? [];
            _logger.LogInformation("Restored {Count} download(s) from {Path}", items.Count, _filePath);
            return items;
        }
        catch (Exception ex)
        {
            // Keep the unreadable file for inspection; otherwise the next save would overwrite it.
            string corruptPath = _filePath + ".corrupt";
            _logger.LogWarning(ex, "Could not read the saved download queue {Path}; starting empty, moved it to {CorruptPath}",
                _filePath, corruptPath);
            TryMove(_filePath, corruptPath);
            return [];
        }
    }

    private void TryMove(string source, string destination)
    {
        try { File.Move(source, destination, overwrite: true); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not move {Path} aside", source); }
    }

    public void Save(IReadOnlyList<DownloadItem> items)
    {
        try
        {
            string json = JsonSerializer.Serialize(new QueueFile { Items = items.ToList() }, SerializerOptions);
            AtomicFile.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save the download queue to {Path}", _filePath);
        }
    }

    /// <summary>On-disk shape; a wrapper object leaves room for future fields.</summary>
    private sealed class QueueFile
    {
        public List<DownloadItem> Items { get; set; } = [];
    }
}
