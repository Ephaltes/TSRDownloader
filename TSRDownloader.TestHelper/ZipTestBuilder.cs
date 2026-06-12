using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TSRDownloader.TestHelper;

/// <summary>Builds real .zip files on disk for extraction tests.</summary>
public static class ZipTestBuilder
{
    /// <summary>Creates a zip at <paramref name="zipPath"/> from entryName → text content.</summary>
    public static string Create(string zipPath, IDictionary<string, string> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (KeyValuePair<string, string> entry in entries)
        {
            ZipArchiveEntry zipEntry = archive.CreateEntry(entry.Key);
            using Stream stream = zipEntry.Open();
            byte[] bytes = Encoding.UTF8.GetBytes(entry.Value);
            stream.Write(bytes, 0, bytes.Length);
        }

        return zipPath;
    }

    /// <summary>Creates a zip containing one benign entry and one zip-slip entry ("../escaped.txt").</summary>
    public static string CreateWithZipSlip(string zipPath, string benignEntry, string slipEntry)
    {
        return Create(zipPath, new Dictionary<string, string>
        {
            [benignEntry] = "benign",
            [slipEntry] = "malicious"
        });
    }

    /// <summary>Reads a zip file's bytes (for serving via the HTTP stub).</summary>
    public static byte[] ReadBytes(string zipPath) => File.ReadAllBytes(zipPath);
}
