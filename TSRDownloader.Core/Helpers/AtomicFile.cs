namespace TSRDownloader.Core.Helpers;

/// <summary>
/// Writes files so that a crash or power loss mid-write never leaves a truncated file behind:
/// the content goes to a temporary sibling first, which then replaces the target in one step.
/// </summary>
internal static class AtomicFile
{
    public static void WriteAllText(string path, string contents)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, contents);
        File.Move(tempPath, path, overwrite: true);
    }
}
