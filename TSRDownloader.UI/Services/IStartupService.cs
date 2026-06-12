namespace TSRDownloader.UI.Services;

/// <summary>
/// Manages whether the application launches automatically when the user logs in.
/// </summary>
public interface IStartupService
{
    /// <summary>Returns whether the application is currently registered to run at startup.</summary>
    bool IsEnabled();

    /// <summary>Enables or disables launching the application at user logon.</summary>
    void SetEnabled(bool enabled);
}
