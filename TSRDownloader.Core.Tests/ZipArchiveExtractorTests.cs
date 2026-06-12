using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.TestHelper;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class ZipArchiveExtractorTests
{
    private static RecordingConflictResolver Overwrite(bool applyToAll = false) =>
        new(new FileConflictResolution(FileConflictAction.Overwrite, applyToAll));

    private static RecordingConflictResolver Skip(bool applyToAll = false) =>
        new(new FileConflictResolution(FileConflictAction.Skip, applyToAll));

    [Fact]
    public async Task ExtractAsync_Should_ExtractAllEntries_When_ArchiveHasNestedDirectories()
    {
        // Arrange
        using TempDir temp = new();
        string zip = ZipTestBuilder.Create(temp.Combine("a.zip"), new Dictionary<string, string>
        {
            ["root.txt"] = "root",
            ["sub/child.txt"] = "child"
        });
        string dest = temp.Combine("out");
        double lastProgress = 0;

        // Act
        await new ZipArchiveExtractor().ExtractAsync(
            zip, dest, Overwrite(), p => lastProgress = p, CancellationToken.None);

        // Assert
        File.ReadAllText(Path.Combine(dest, "root.txt")).ShouldBe("root");
        File.ReadAllText(Path.Combine(dest, "sub", "child.txt")).ShouldBe("child");
        lastProgress.ShouldBe(100);
    }

    [Fact]
    public async Task ExtractAsync_Should_OverwriteFile_When_ResolverReturnsOverwrite()
    {
        // Arrange
        using TempDir temp = new();
        string zip = ZipTestBuilder.Create(temp.Combine("a.zip"),
            new Dictionary<string, string> { ["f.txt"] = "new" });
        string dest = temp.Combine("out");
        Directory.CreateDirectory(dest);
        File.WriteAllText(Path.Combine(dest, "f.txt"), "old");
        RecordingConflictResolver resolver = Overwrite();

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, dest, resolver, null, CancellationToken.None);

        // Assert
        File.ReadAllText(Path.Combine(dest, "f.txt")).ShouldBe("new");
        resolver.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task ExtractAsync_Should_KeepFile_When_ResolverReturnsSkip()
    {
        // Arrange
        using TempDir temp = new();
        string zip = ZipTestBuilder.Create(temp.Combine("a.zip"),
            new Dictionary<string, string> { ["f.txt"] = "new" });
        string dest = temp.Combine("out");
        Directory.CreateDirectory(dest);
        File.WriteAllText(Path.Combine(dest, "f.txt"), "old");

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, dest, Skip(), null, CancellationToken.None);

        // Assert
        File.ReadAllText(Path.Combine(dest, "f.txt")).ShouldBe("old");
    }

    [Fact]
    public async Task ExtractAsync_Should_AskResolverOnce_When_ResolutionAppliesToAll()
    {
        // Arrange
        using TempDir temp = new();
        string zip = ZipTestBuilder.Create(temp.Combine("a.zip"), new Dictionary<string, string>
        {
            ["f1.txt"] = "new1",
            ["f2.txt"] = "new2"
        });
        string dest = temp.Combine("out");
        Directory.CreateDirectory(dest);
        File.WriteAllText(Path.Combine(dest, "f1.txt"), "old1");
        File.WriteAllText(Path.Combine(dest, "f2.txt"), "old2");
        RecordingConflictResolver resolver = Skip(applyToAll: true);

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, dest, resolver, null, CancellationToken.None);

        // Assert
        resolver.Calls.ShouldBe(1);
        File.ReadAllText(Path.Combine(dest, "f1.txt")).ShouldBe("old1");
        File.ReadAllText(Path.Combine(dest, "f2.txt")).ShouldBe("old2");
    }

    [Fact]
    public async Task ExtractAsync_Should_SkipEntry_When_EntryEscapesDestination()
    {
        // Arrange
        using TempDir temp = new();
        string zip = ZipTestBuilder.CreateWithZipSlip(temp.Combine("a.zip"), "safe.txt", "../escaped.txt");
        string dest = temp.Combine("out");

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, dest, Overwrite(), null, CancellationToken.None);

        // Assert
        File.Exists(Path.Combine(dest, "safe.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(temp.Path, "escaped.txt")).ShouldBeFalse();
    }

    [Fact]
    public async Task ExtractAsync_Should_Throw_When_CancellationRequested()
    {
        // Arrange
        using TempDir temp = new();
        string zip = ZipTestBuilder.Create(temp.Combine("a.zip"),
            new Dictionary<string, string> { ["f.txt"] = "x" });
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act / Assert
        await Should.ThrowAsync<System.OperationCanceledException>(() =>
            new ZipArchiveExtractor().ExtractAsync(zip, temp.Combine("out"), Overwrite(), null, cts.Token));
    }
}
