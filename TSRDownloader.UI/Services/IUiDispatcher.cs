namespace TSRDownloader.UI.Services;

/// <summary>Marshals an action onto the UI thread. Abstracted so view models are testable.</summary>
public interface IUiDispatcher
{
    void Invoke(Action action);
}
