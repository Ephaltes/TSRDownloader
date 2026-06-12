namespace TSRDownloader.Core.Services;

/// <summary>Extracts an archive to a destination directory, reporting progress (0–100).</summary>
public interface IArchiveExtractor
{
    /// <summary>
    /// Extracts <paramref name="archivePath"/> into <paramref name="destinationDirectory"/>.
    /// File-exists conflicts are delegated to <paramref name="conflictResolver"/>.
    /// </summary>
    Task ExtractAsync(
        string archivePath,
        string destinationDirectory,
        IFileConflictResolver conflictResolver,
        Action<double>? onProgress,
        CancellationToken ct);
}
