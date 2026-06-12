using System.Threading;
using System.Threading.Tasks;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;

namespace TSRDownloader.TestHelper;

/// <summary>Returns a fixed resolution and counts how often it was asked.</summary>
public sealed class RecordingConflictResolver : IFileConflictResolver
{
    private readonly FileConflictResolution _result;

    public int Calls { get; private set; }

    public RecordingConflictResolver(FileConflictResolution result) => _result = result;

    public Task<FileConflictResolution> ResolveAsync(
        string relativeEntryName, string destinationPath, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(_result);
    }
}
