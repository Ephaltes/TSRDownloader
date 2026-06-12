using TSRDownloader.UI.ViewModels;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Opens and closes the update window. Abstracted so the update flow is testable without WPF.
/// </summary>
public interface IUpdateWindowService
{
    /// <summary>Shows a (non-modal) update window bound to <paramref name="viewModel"/>.</summary>
    void Show(UpdateViewModel viewModel);

    /// <summary>Closes the window bound to <paramref name="viewModel"/>, if it is still open.</summary>
    void Close(UpdateViewModel viewModel);
}
