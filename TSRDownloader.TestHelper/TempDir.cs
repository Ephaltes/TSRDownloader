using System;
using System.IO;

namespace TSRDownloader.TestHelper;

/// <summary>A unique temp directory that deletes itself on dispose.</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tsrtests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Combine(params string[] parts)
    {
        string[] all = new string[parts.Length + 1];
        all[0] = Path;
        Array.Copy(parts, 0, all, 1, parts.Length);
        return System.IO.Path.Combine(all);
    }

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); }
        catch { /* best-effort cleanup */ }
    }
}
