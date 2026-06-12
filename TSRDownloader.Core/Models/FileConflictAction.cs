namespace TSRDownloader.Core.Models;

/// <summary>How to resolve a file that already exists at the extraction target.</summary>
public enum FileConflictAction
{
    /// <summary>Overwrite the existing file with the archive entry.</summary>
    Overwrite,
    /// <summary>Keep the existing file and skip the archive entry.</summary>
    Skip
}
