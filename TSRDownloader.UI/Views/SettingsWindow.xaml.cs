using System.Text.RegularExpressions;
using TSRDownloader.Core.Models;
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

        _viewModel = ((App)Application.Current).Services.GetRequiredService<SettingsViewModel>();

        _viewModel.CloseRequested += () => DialogResult = true;
        DataContext = _viewModel;
    }

    /// <summary>Restricts input to digits only and validates the allowed range on each keystroke.</summary>
    private void OnMaxActiveDownloadsPreviewTextInput(object sender, TextCompositionEventArgs eventArgs) =>
        eventArgs.Handled = !IsAcceptedNumberInput(MaxDownloadsTextBox, eventArgs.Text,
            AppConfiguration.MinActiveDownloads, AppConfiguration.MaxActiveDownloadsLimit);

    /// <summary>Restricts input to digits only and validates the ticket delay range on each keystroke.</summary>
    private void OnTicketDelayPreviewTextInput(object sender, TextCompositionEventArgs eventArgs) =>
        eventArgs.Handled = !IsAcceptedNumberInput(TicketDelayTextBox, eventArgs.Text,
            AppConfiguration.MinTicketActivationDelaySeconds, AppConfiguration.MaxTicketActivationDelaySeconds);

    /// <summary>
    /// Whether typing <paramref name="input"/> into <paramref name="textBox"/> keeps it a whole
    /// number within [<paramref name="minimum"/>, <paramref name="maximum"/>].
    /// </summary>
    private static bool IsAcceptedNumberInput(TextBox textBox, string input, int minimum, int maximum)
    {
        // Only allow digit characters
        if (!DigitsOnlyRegex().IsMatch(input))
            return false;

        // Build what the text would look like after this input
        // (insertion at the caret, or replacement if text is selected)
        string newText = textBox.SelectionLength > 0
            ? textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength).Insert(textBox.SelectionStart, input)
            : textBox.Text.Insert(textBox.CaretIndex, input);

        return int.TryParse(newText, out int value) && value >= minimum && value <= maximum;
    }

    [GeneratedRegex(@"^\d$")]
    private static partial Regex DigitsOnlyRegex();
}
