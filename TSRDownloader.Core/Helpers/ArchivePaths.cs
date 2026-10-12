namespace TSRDownloader.Core.Helpers;

/// <summary>Where downloaded archives are extracted to, defined once for the downloader, the extractor and the UI.</summary>
public static class ArchivePaths
{
    /// <summary>Whether <paramref name="path"/> is a .zip archive (by extension).</summary>
    public static bool IsZip(string path) =>
        Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    /// <summary>The folder an archive is extracted into: next to it, named after it without the extension.</summary>
    public static string ExtractionFolder(string archivePath) =>
        Path.Combine(Path.GetDirectoryName(archivePath) ?? string.Empty, Path.GetFileNameWithoutExtension(archivePath));
}
