using CommunityToolkit.WinUI.Notifications;
using Microsoft.Extensions.Logging;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Shows Windows toast notifications via the Windows Community Toolkit
/// <see cref="ToastContentBuilder"/>. For unpackaged Win32 apps this uses the
/// compat shim under the hood and needs no registration or COM activation setup.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const string ActionKey = "action";

    private readonly ILogger<NotificationService> _logger;

    public event Action<string>? ActionInvoked;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;

        // Also fires when the app was launched by clicking a toast button while it was closed.
        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
    }

    public void Notify(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show notification {Title}", title);
        }
    }

    public void NotifyWithAction(string title, string message, string buttonText, string actionArgument)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddButton(new ToastButton()
                    .SetContent(buttonText)
                    .AddArgument(ActionKey, actionArgument))
                .Show();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show notification {Title}", title);
        }
    }

    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        try
        {
            ToastArguments arguments = ToastArguments.Parse(toastArgs.Argument);
            if (arguments.TryGetValue(ActionKey, out string actionArgument))
                ActionInvoked?.Invoke(actionArgument);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle notification activation {Argument}", toastArgs.Argument);
        }
    }
}
