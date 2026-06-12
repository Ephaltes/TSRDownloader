using TSRDownloader.UI.ViewModels;

namespace TSRDownloader.UI.Views;

/// <summary>
/// Shows update download progress (with Cancel) or why an update cannot run right now (with Close).
/// </summary>
public partial class UpdateWindow : Window
{
    private readonly UpdateViewModel _viewModel;
    private bool _closeRequested;

    public UpdateWindow(UpdateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.CloseRequested += OnCloseRequested;
        Closing += OnClosing;
    }

    /// <summary>Closes the window from code (e.g. after a failed download) without cancelling anything.</summary>
    public void CloseQuietly()
    {
        _closeRequested = true;
        Close();
    }

    private void OnCloseRequested()
    {
        _closeRequested = true;
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Closing via the title bar during a download counts as Cancel.
        if (!_closeRequested && !_viewModel.IsBlocked)
            _viewModel.CancelCommand.Execute(null);

        _viewModel.CloseRequested -= OnCloseRequested;
    }
}
