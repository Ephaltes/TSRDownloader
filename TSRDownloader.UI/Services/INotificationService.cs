namespace TSRDownloader.UI.Services;

/// <summary>
/// Shows lightweight Windows notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>Raised (on a background thread) with the action argument when a notification button is clicked.</summary>
    event Action<string>? ActionInvoked;

    /// <summary>Shows a notification with the given title and message.</summary>
    void Notify(string title, string message);

    /// <summary>
    /// Shows a notification with a button; clicking it raises <see cref="ActionInvoked"/>
    /// with <paramref name="actionArgument"/>.
    /// </summary>
    void NotifyWithAction(string title, string message, string buttonText, string actionArgument);
}
