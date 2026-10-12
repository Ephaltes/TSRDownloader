namespace TSRDownloader.UI.Services;

/// <summary>
/// Shows lightweight Windows notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>Tag for "link found" notifications.</summary>
    const string LinkFoundTag = "link-found";

    /// <summary>Tag shared by "URL copied" and "queue finished" so they replace each other.</summary>
    const string StatusTag = "status";

    /// <summary>Raised (on a background thread) with the action argument when a notification button is clicked.</summary>
    event Action<string>? ActionInvoked;

    /// <summary>
    /// Shows a notification with the given title and message. A notification with the same
    /// <paramref name="tag"/> replaces the previous one instead of queuing behind it.
    /// </summary>
    void Notify(string title, string message, string? tag = null);

    /// <summary>
    /// Shows a notification with a button; clicking it raises <see cref="ActionInvoked"/>
    /// with <paramref name="actionArgument"/>.
    /// </summary>
    void NotifyWithAction(string title, string message, string buttonText, string actionArgument);
}
