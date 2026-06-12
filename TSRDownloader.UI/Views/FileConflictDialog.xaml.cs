using TSRDownloader.Core.Models;
using TSRDownloader.UI.Localization;

namespace TSRDownloader.UI.Views;

/// <summary>
/// Modal dialog asking whether to overwrite or skip an existing file during extraction,
/// with an option to apply the choice to the whole archive.
/// </summary>
public partial class FileConflictDialog : Window
{
    /// <summary>The action chosen by the user. Defaults to Skip (safe) if the dialog is closed.</summary>
    public FileConflictAction SelectedAction { get; private set; } = FileConflictAction.Skip;

    /// <summary>Whether the choice applies to every remaining conflict in this archive.</summary>
    public bool ApplyToAll => ApplyToAllCheckBox.IsChecked == true;

    public FileConflictDialog(string fileName)
    {
        InitializeComponent();
        MessageText.Text = LocalizationManager.Instance.Format("Conflict_Message", fileName);
    }

    private void OnOverwrite(object sender, RoutedEventArgs e)
    {
        SelectedAction = FileConflictAction.Overwrite;
        DialogResult = true;
    }

    private void OnSkip(object sender, RoutedEventArgs e)
    {
        SelectedAction = FileConflictAction.Skip;
        DialogResult = true;
    }
}
