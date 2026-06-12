using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;

namespace TSRDownloader.TestHelper;

/// <summary>
/// Fakes the TSR client for integration tests: resolves to a fixed file name and,
/// on download, copies a prepared source zip into the destination directory.
/// </summary>
public sealed class FakeTsrDownloadClient : ITsrDownloadClient
{
    private readonly string _sourceZipPath;
    private readonly string _fileName;

    public FakeTsrDownloadClient(string sourceZipPath, string fileName)
    {
        _sourceZipPath = sourceZipPath;
        _fileName = fileName;
    }

    public Task<TsrDownloadInfo> ResolveDownloadAsync(int itemId, CancellationToken ct) =>
        Task.FromResult(new TsrDownloadInfo(itemId, "https://cdn.example.test/file.zip", _fileName));

    public Task<string> DownloadFileAsync(
        TsrDownloadInfo info, string destinationDirectory, Action<DownloadProgress>? onProgress, CancellationToken ct)
    {
        Directory.CreateDirectory(destinationDirectory);
        string target = Path.Combine(destinationDirectory, info.FileName);
        File.Copy(_sourceZipPath, target, overwrite: true);

        long size = new FileInfo(target).Length;
        onProgress?.Invoke(new DownloadProgress(size, size));
        return Task.FromResult(target);
    }
}
