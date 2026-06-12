using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Views;

namespace TSRDownloader.UI.Services;

/// <summary>
/// UI implementation of <see cref="IFileConflictResolver"/>. Shows a modal
/// <see cref="FileConflictDialog"/> on the UI thread and returns the user's choice.
/// Because extraction is serialized, at most one dialog is ever shown at a time.
/// </summary>
public sealed class FileConflictResolver : IFileConflictResolver
{
    public Task<FileConflictResolution> ResolveAsync(
        string relativeEntryName, string destinationPath, CancellationToken ct)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            FileConflictDialog dialog = new(relativeEntryName)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
            return new FileConflictResolution(dialog.SelectedAction, dialog.ApplyToAll);
        }).Task;
    }
}
