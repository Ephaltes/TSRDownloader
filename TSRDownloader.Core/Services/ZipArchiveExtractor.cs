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
        CancellationToken ct)
    {
        Directory.CreateDirectory(destinationDirectory);
        string destRoot = Path.GetFullPath(destinationDirectory);

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
            ct.ThrowIfCancellationRequested();

            string targetPath = Path.GetFullPath(Path.Combine(destRoot, entry.FullName));

            // Zip-slip guard: the resolved target must stay inside the destination root.
            bool insideRoot =
                targetPath.StartsWith(destRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if (!insideRoot)
            {
                processedBytes += entry.Length;
                onProgress?.Invoke(Percentage(processedBytes, totalBytes));
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            if (File.Exists(targetPath))
            {
                FileConflictAction action;
                if (applyToAllAction is { } cached)
                {
                    action = cached;
                }
                else
                {
                    FileConflictResolution resolution =
                        await conflictResolver.ResolveAsync(entry.FullName, targetPath, ct);
                    action = resolution.Action;
                    if (resolution.ApplyToAll)
                        applyToAllAction = action;
                }

                if (action == FileConflictAction.Skip)
                {
                    processedBytes += entry.Length;
                    onProgress?.Invoke(Percentage(processedBytes, totalBytes));
                    continue;
                }
            }

            await using (Stream entryStream = entry.Open())
            await using (FileStream outStream = new(
                targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: BufferSize, useAsync: true))
            {
                await entryStream.CopyToAsync(outStream, BufferSize, ct);
            }

            processedBytes += entry.Length;
            onProgress?.Invoke(Percentage(processedBytes, totalBytes));
        }

        onProgress?.Invoke(100);
    }

    private static double Percentage(long processed, long total) =>
        Math.Round((double)processed / total * 100, 1);
}
