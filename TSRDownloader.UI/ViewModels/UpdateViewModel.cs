using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TSRDownloader.UI.ViewModels;

/// <summary>
/// ViewModel for the update window: either download progress with a Cancel button, or a
/// blocked message (e.g. downloads still running) with a Close button.
/// </summary>
public partial class UpdateViewModel : ObservableObject
{
    private readonly CancellationTokenSource _cancellation = new();

    [ObservableProperty]
    private int _progress;

    /// <summary>The text shown to the user.</summary>
    public string Message { get; }

    /// <summary>True when the update cannot run; the window only shows <see cref="Message"/> and a Close button.</summary>
    public bool IsBlocked { get; }

    /// <summary>Cancelled when the user cancels (or closes the window during a download).</summary>
    public CancellationToken CancellationToken => _cancellation.Token;

    /// <summary>Raised when the window should close.</summary>
    public event Action? CloseRequested;

    public UpdateViewModel(string message, bool isBlocked)
    {
        Message = message;
        IsBlocked = isBlocked;
    }

    [RelayCommand]
    private void Cancel()
    {
        _cancellation.Cancel();
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
