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
        using TemporaryDirectory temporaryDirectory = new();
        string zip = ZipTestBuilder.Create(temporaryDirectory.Combine("a.zip"), new Dictionary<string, string>
        {
            ["root.txt"] = "root",
            ["sub/child.txt"] = "child"
        });
        string destination = temporaryDirectory.Combine("out");
        double lastProgress = 0;

        // Act
        await new ZipArchiveExtractor().ExtractAsync(
            zip, destination, Overwrite(), progress => lastProgress = progress, CancellationToken.None);

        // Assert
        File.ReadAllText(Path.Combine(destination, "root.txt")).ShouldBe("root");
        File.ReadAllText(Path.Combine(destination, "sub", "child.txt")).ShouldBe("child");
        lastProgress.ShouldBe(100);
    }

    [Fact]
    public async Task ExtractAsync_Should_OverwriteFile_When_ResolverReturnsOverwrite()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string zip = ZipTestBuilder.Create(temporaryDirectory.Combine("a.zip"),
            new Dictionary<string, string> { ["f.txt"] = "new" });
        string destination = temporaryDirectory.Combine("out");
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(destination, "f.txt"), "old");
        RecordingConflictResolver resolver = Overwrite();

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, destination, resolver, null, CancellationToken.None);

        // Assert
        File.ReadAllText(Path.Combine(destination, "f.txt")).ShouldBe("new");
        resolver.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task ExtractAsync_Should_KeepFile_When_ResolverReturnsSkip()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string zip = ZipTestBuilder.Create(temporaryDirectory.Combine("a.zip"),
            new Dictionary<string, string> { ["f.txt"] = "new" });
        string destination = temporaryDirectory.Combine("out");
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(destination, "f.txt"), "old");

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, destination, Skip(), null, CancellationToken.None);

        // Assert
        File.ReadAllText(Path.Combine(destination, "f.txt")).ShouldBe("old");
    }

    [Fact]
    public async Task ExtractAsync_Should_AskResolverOnce_When_ResolutionAppliesToAll()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string zip = ZipTestBuilder.Create(temporaryDirectory.Combine("a.zip"), new Dictionary<string, string>
        {
            ["f1.txt"] = "new1",
            ["f2.txt"] = "new2"
        });
        string destination = temporaryDirectory.Combine("out");
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(destination, "f1.txt"), "old1");
        File.WriteAllText(Path.Combine(destination, "f2.txt"), "old2");
        RecordingConflictResolver resolver = Skip(applyToAll: true);

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, destination, resolver, null, CancellationToken.None);

        // Assert
        resolver.Calls.ShouldBe(1);
        File.ReadAllText(Path.Combine(destination, "f1.txt")).ShouldBe("old1");
        File.ReadAllText(Path.Combine(destination, "f2.txt")).ShouldBe("old2");
    }

    [Fact]
    public async Task ExtractAsync_Should_SkipEntry_When_EntryEscapesDestination()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string zip = ZipTestBuilder.CreateWithZipSlip(temporaryDirectory.Combine("a.zip"), "safe.txt", "../escaped.txt");
        string destination = temporaryDirectory.Combine("out");

        // Act
        await new ZipArchiveExtractor().ExtractAsync(zip, destination, Overwrite(), null, CancellationToken.None);

        // Assert
        File.Exists(Path.Combine(destination, "safe.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(temporaryDirectory.Path, "escaped.txt")).ShouldBeFalse();
    }

    [Fact]
    public async Task ExtractAsync_Should_Throw_When_CancellationRequested()
    {
        // Arrange
        using TemporaryDirectory temporaryDirectory = new();
        string zip = ZipTestBuilder.Create(temporaryDirectory.Combine("a.zip"),
            new Dictionary<string, string> { ["f.txt"] = "x" });
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act / Assert
        await Should.ThrowAsync<System.OperationCanceledException>(() =>
            new ZipArchiveExtractor().ExtractAsync(zip, temporaryDirectory.Combine("out"), Overwrite(), null, cancellation.Token));
    }
}
