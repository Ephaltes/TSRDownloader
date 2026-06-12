namespace TSRDownloader.Core.Models;

/// <summary>
/// The user's answer to a file conflict: the chosen action and whether it should
/// apply to every remaining conflict in the current archive.
/// </summary>
public readonly record struct FileConflictResolution(FileConflictAction Action, bool ApplyToAll);
