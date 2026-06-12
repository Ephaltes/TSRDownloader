using System.Text.RegularExpressions;
using TSRDownloader.UI.ViewModels;

namespace TSRDownloader.UI.Views;

/// <summary>
/// Settings dialog window.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();

        // Resolve ViewModel from DI container
        _viewModel = ((App)Application.Current).Services.GetService<SettingsViewModel>()
                     ?? throw new InvalidOperationException("SettingsViewModel not registered in DI.");

        _viewModel.CloseRequested += () => DialogResult = true;
        DataContext = _viewModel;
    }

    /// <summary>Restricts input to digits only and validates range 1–10 on each keystroke.</summary>
    private void MaxDownloadsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !IsAcceptedNumberInput(MaxDownloadsTextBox, e.Text,
            SettingsViewModel.MinActiveDownloads, SettingsViewModel.MaxActiveDownloadsLimit);

    /// <summary>Restricts input to digits only and validates the ticket delay range on each keystroke.</summary>
    private void TicketDelayTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !IsAcceptedNumberInput(TicketDelayTextBox, e.Text,
            SettingsViewModel.MinTicketActivationDelaySeconds, SettingsViewModel.MaxTicketActivationDelaySeconds);

    /// <summary>
    /// Whether typing <paramref name="input"/> into <paramref name="textBox"/> keeps it a whole
    /// number within [<paramref name="min"/>, <paramref name="max"/>].
    /// </summary>
    private static bool IsAcceptedNumberInput(TextBox textBox, string input, int min, int max)
    {
        // Only allow digit characters
        if (!DigitsOnlyRegex().IsMatch(input))
            return false;

        // Build what the text would look like after this input
        // (insertion at the caret, or replacement if text is selected)
        string newText = textBox.SelectionLength > 0
            ? textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength).Insert(textBox.SelectionStart, input)
            : textBox.Text.Insert(textBox.CaretIndex, input);

        return int.TryParse(newText, out int value) && value >= min && value <= max;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\d$")]
    private static partial Regex DigitsOnlyRegex();
}
