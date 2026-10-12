using System.IO.Compression;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Extracts .zip archives using the built-in <see cref="ZipArchive"/>.
/// Progress is byte-weighted over uncompressed entry sizes. A cached "apply to all"
/// decision short-circuits the conflict resolver for the rest of the archive.
/// Guards against zip-slip (entries that would escape the destination directory).
/// </summary>
public sealed class ZipArchiveExtractor : IArchiveExtractor
{
    private const int BufferSize = 128 * 1024;

    public async Task ExtractAsync(
        string archivePath,
        string destinationDirectory,
        IFileConflictResolver conflictResolver,
        Action<double>? onProgress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);
        string destinationRoot = Path.GetFullPath(destinationDirectory);

        using ZipArchive archive = ZipFile.OpenRead(archivePath);

        // Directory entries have an empty Name; only real files carry payload/progress.
        List<ZipArchiveEntry> fileEntries =
            archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToList();

        long totalBytes = fileEntries.Sum(entry => entry.Length);
        if (totalBytes <= 0)
            totalBytes = 1; // avoid divide-by-zero for empty/zero-length archives

        long processedBytes = 0;
        FileConflictAction? applyToAllAction = null;

        foreach (ZipArchiveEntry entry in fileEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string targetPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));

            // Zip-slip guard: entries that would land outside the destination are skipped.
            if (IsInside(destinationRoot, targetPath) && await ShouldWriteAsync(entry, targetPath))
                await WriteEntryAsync(entry, targetPath, cancellationToken);

            processedBytes += entry.Length;
            onProgress?.Invoke(Percentage(processedBytes, totalBytes));
        }

        onProgress?.Invoke(100);

        // An existing file is only overwritten if the user (or an earlier "apply to all") says so.
        async Task<bool> ShouldWriteAsync(ZipArchiveEntry entry, string targetPath)
        {
            if (!File.Exists(targetPath))
                return true;

            if (applyToAllAction is { } rememberedAction)
                return rememberedAction != FileConflictAction.Skip;

            FileConflictResolution resolution =
                await conflictResolver.ResolveAsync(entry.FullName, targetPath, cancellationToken);
            if (resolution.ApplyToAll)
                applyToAllAction = resolution.Action;
            return resolution.Action != FileConflictAction.Skip;
        }
    }

    private static bool IsInside(string rootDirectory, string path) =>
        path.StartsWith(rootDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static async Task WriteEntryAsync(ZipArchiveEntry entry, string targetPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        await using Stream entryStream = entry.Open();
        await using FileStream fileStream = new(
            targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: BufferSize, useAsync: true);
        await entryStream.CopyToAsync(fileStream, BufferSize, cancellationToken);
    }

    private static double Percentage(long processed, long total) =>
        Math.Round((double)processed / total * 100, 1);
}
