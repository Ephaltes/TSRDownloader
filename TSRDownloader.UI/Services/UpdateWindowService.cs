using TSRDownloader.UI.ViewModels;
using TSRDownloader.UI.Views;

namespace TSRDownloader.UI.Services;

/// <summary>Production <see cref="IUpdateWindowService"/> that creates <see cref="UpdateWindow"/> instances.</summary>
public sealed class UpdateWindowService : IUpdateWindowService
{
    private readonly Dictionary<UpdateViewModel, UpdateWindow> _openWindows = [];

    public void Show(UpdateViewModel viewModel)
    {
        UpdateWindow window = new(viewModel);
        window.Closed += (_, _) => _openWindows.Remove(viewModel);
        _openWindows[viewModel] = window;
        window.Show();
        window.Activate();
    }

    public void Close(UpdateViewModel viewModel)
    {
        if (_openWindows.TryGetValue(viewModel, out UpdateWindow? window))
            window.CloseQuietly();
    }
}
