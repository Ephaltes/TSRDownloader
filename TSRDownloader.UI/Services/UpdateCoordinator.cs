using Microsoft.Extensions.Logging;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.ViewModels;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Decides what happens with application updates based on <see cref="AppConfig.UpdateMode"/>:
/// install automatically at startup, notify via a toast with an "Install now" button, or do nothing.
/// Every failure is logged and swallowed — the updater never prevents the app from starting.
/// </summary>
public sealed class UpdateCoordinator
{
    /// <summary>Toast action argument for the "Install now" button.</summary>
    public const string InstallAction = "install-update";

    private static readonly TimeSpan DefaultCheckTimeout = TimeSpan.FromSeconds(5);

    private readonly IUpdateService _updateService;
    private readonly IConfigService _configService;
    private readonly IDownloadService _downloadService;
    private readonly INotificationService _notificationService;
    private readonly IUpdateWindowService _windowService;
    private readonly LocalizationManager _localization;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<UpdateCoordinator> _logger;
    private readonly TimeSpan _checkTimeout;

    private AvailableUpdate? _notifiedUpdate;
    private bool _isUpdating;

    public UpdateCoordinator(
        IUpdateService updateService,
        IConfigService configService,
        IDownloadService downloadService,
        INotificationService notificationService,
        IUpdateWindowService windowService,
        LocalizationManager localization,
        IUiDispatcher dispatcher,
        ILogger<UpdateCoordinator> logger,
        TimeSpan? checkTimeout = null)
    {
        _updateService = updateService;
        _configService = configService;
        _downloadService = downloadService;
        _notificationService = notificationService;
        _windowService = windowService;
        _localization = localization;
        _dispatcher = dispatcher;
        _logger = logger;
        _checkTimeout = checkTimeout ?? DefaultCheckTimeout;

        _notificationService.ActionInvoked += OnNotificationActionInvoked;
    }

    private UpdateMode Mode => _configService.Config.UpdateMode;

    /// <summary>
    /// Automatic mode, before the main window opens: downloads and applies a newer release.
    /// Returns true when the update is being applied (the process exits); false to continue starting.
    /// </summary>
    public async Task<bool> RunStartupUpdateAsync()
    {
        if (!_updateService.IsInstalled || Mode != UpdateMode.Automatic)
            return false;

        AvailableUpdate? update = await TryCheckAsync();
        if (update is null)
            return false;

        return await DownloadAndApplyAsync(update);
    }

    /// <summary>Notify-only mode: checks in the background and shows a toast if a newer release exists.</summary>
    public async Task CheckAndNotifyAsync()
    {
        if (!_updateService.IsInstalled || Mode != UpdateMode.NotifyOnly)
            return;

        AvailableUpdate? update = await TryCheckAsync();
        if (update is null)
            return;

        _notifiedUpdate = update;
        _notificationService.NotifyWithAction(
            _localization.Get("Toast_UpdateAvailable_Title"),
            _localization.Format("Toast_UpdateAvailable_Message", update.Version),
            _localization.Get("Toast_UpdateAvailable_Install"),
            InstallAction);
    }

    /// <summary>
    /// "Install now": downloads and applies the update, unless downloads are still running.
    /// Returns true when the update is being applied.
    /// </summary>
    public async Task<bool> InstallAsync()
    {
        if (!_updateService.IsInstalled || _isUpdating)
            return false;

        if (HasActiveDownloads())
        {
            _logger.LogInformation("Update postponed: downloads are still in progress");
            _windowService.Show(new UpdateViewModel(_localization.Get("Update_DownloadsActive"), isBlocked: true));
            return false;
        }

        // After a restart (e.g. the app was launched by the toast) nothing is remembered yet.
        AvailableUpdate? update = _notifiedUpdate ?? await TryCheckAsync();
        if (update is null)
            return false;

        return await DownloadAndApplyAsync(update);
    }

    private void OnNotificationActionInvoked(string action)
    {
        if (action == InstallAction)
            _dispatcher.Invoke(() => _ = InstallAsync());
    }

    private bool HasActiveDownloads() =>
        _downloadService.Items.Any(downloadItem => downloadItem.Status is
            DownloadStatus.Queued or DownloadStatus.Downloading or
            DownloadStatus.WaitingToExtract or DownloadStatus.Extracting);

    private async Task<AvailableUpdate?> TryCheckAsync()
    {
        using CancellationTokenSource timeout = new(_checkTimeout);
        try
        {
            // WaitAsync enforces the timeout even if the check ignores the token.
            AvailableUpdate? update = await _updateService.CheckAsync(timeout.Token).WaitAsync(timeout.Token);

            if (update is null)
                _logger.LogInformation("No update available (current version {Version})", _updateService.CurrentVersion);
            else
                _logger.LogInformation("Update {NewVersion} available (current version {Version})",
                    update.Version, _updateService.CurrentVersion);

            return update;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Update check timed out after {Timeout}", _checkTimeout);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            return null;
        }
    }

    private async Task<bool> DownloadAndApplyAsync(AvailableUpdate update)
    {
        _isUpdating = true;
        UpdateViewModel viewModel = new(_localization.Format("Update_Downloading", update.Version), isBlocked: false);
        _windowService.Show(viewModel);

        try
        {
            await _updateService.DownloadAsync(
                update,
                progress => _dispatcher.Invoke(() => viewModel.Progress = progress),
                viewModel.CancellationToken);

            _logger.LogInformation("Applying update {Version} and restarting", update.Version);
            _updateService.ApplyAndRestart(update);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update {Version} cancelled by the user", update.Version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update {Version} failed", update.Version);
        }
        finally
        {
            _isUpdating = false;
        }

        _windowService.Close(viewModel);
        return false;
    }
}
