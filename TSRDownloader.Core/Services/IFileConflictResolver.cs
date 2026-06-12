using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Asks how to resolve a file that already exists during extraction.
/// Implemented by the UI layer (shows a modal dialog).
/// </summary>
public interface IFileConflictResolver
{
    /// <summary>
    /// Resolves a single conflict. Called only when the target file already exists.
    /// </summary>
    /// <param name="relativeEntryName">The archive-relative entry name (e.g. "sub/file.package").</param>
    /// <param name="destinationPath">The absolute path that already exists on disk.</param>
    Task<FileConflictResolution> ResolveAsync(
        string relativeEntryName, string destinationPath, CancellationToken ct);
}
