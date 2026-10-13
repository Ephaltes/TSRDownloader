using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Presentation;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI.ViewModels;

/// <summary>
/// ViewModel wrapping a DownloadItem for UI binding.
/// Uses CommunityToolkit.Mvvm source generators for INotifyPropertyChanged.
/// </summary>
public partial class DownloadItemViewModel : ObservableObject
{
    private readonly DownloadItem _model;
    private readonly INotificationService _notificationService;
    private readonly TransferRateMeter _transferRateMeter = new();

    public DownloadItem Model => _model;

    public int ItemId => _model.ItemId;
    public string Url => _model.Url;
    public string FileName => _model.FileName ?? "-";

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isIndeterminate;

    /// <summary>The full error message of a failed item, which the progress cell may cut off; null otherwise (no tooltip).</summary>
    [ObservableProperty]
    private string? _errorToolTip;

    [ObservableProperty]
    private string _statusIcon = string.Empty;

    [ObservableProperty]
    private Brush _statusColor = Brushes.Gray;

    public DownloadItemViewModel(DownloadItem model, INotificationService notificationService)
    {
        _model = model;
        _notificationService = notificationService;
        UpdateFromModel();
    }

    /// <summary>
    /// Refreshes every displayed property from the underlying model.
    /// Called when the download service reports changes and when the language changes.
    /// </summary>
    public void UpdateFromModel()
    {
        DownloadStatusPresentation presentation = DownloadStatusPresenter.For(_model.Status);

        if (IsTransferring)
            _transferRateMeter.Sample(_model.DownloadedBytes, Environment.TickCount64);
        else
            _transferRateMeter.Reset();

        StatusIcon = presentation.Icon;
        StatusText = LocalizationManager.Instance.Get(presentation.LabelKey);
        StatusColor = presentation.Color;
        IsIndeterminate = presentation.IsIndeterminate || IsWaitingForTransfer;

        Progress = _model.Status.IsSucceeded() ? 100 : _model.Progress;
        ProgressText = BuildProgressText();
        ErrorToolTip = _model.Status.IsFailed() ? ProgressText : null;
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(StatusSortOrder));
    }

    /// <summary>Position among the queued items (1 = next to start); 0 when not queued.</summary>
    [ObservableProperty]
    private int _queuePosition;

    partial void OnQueuePositionChanged(int value) => ProgressText = BuildProgressText();

    // Downloading but no bytes yet: TSR's ticket handshake (or a retry wait) is still running.
    private bool IsWaitingForTransfer =>
        _model.Status == DownloadStatus.Downloading && _model.DownloadedBytes == 0 && _model.TotalBytes == 0;

    private bool IsTransferring => _model.Status == DownloadStatus.Downloading && !IsWaitingForTransfer;

    private string BuildProgressText() => _model.Status switch
    {
        DownloadStatus.Queued => QueuePosition > 0
            ? LocalizationManager.Instance.Format("Progress_InQueue", QueuePosition)
            : LocalizationManager.Instance.Get("Progress_QueuePosition"),
        DownloadStatus.Downloading => IsWaitingForTransfer ? WaitingText() : TransferText(),
        DownloadStatus.Completed => "100%",
        DownloadStatus.Failed => _model.ErrorMessage ?? LocalizationManager.Instance.Get("Progress_UnknownError"),
        DownloadStatus.WaitingToExtract => LocalizationManager.Instance.Get("Progress_WaitingToExtract"),
        DownloadStatus.Extracting => $"{_model.Progress:F1}%",
        DownloadStatus.ExtractionCompleted => LocalizationManager.Instance.Get("Progress_ExtractionDone"),
        DownloadStatus.ExtractionFailed => _model.ErrorMessage ?? LocalizationManager.Instance.Get("Progress_ExtractionError"),
        _ => string.Empty
    };

    private string WaitingText()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        return _model.RetryAttempt > 0
            ? localization.Format("Progress_Retrying", _model.RetryAttempt, DownloadService.MaxAutomaticRetries)
            : localization.Get("Progress_WaitingForTsr");
    }

    /// <summary>"1.2 / 4.2 MB · 1.1 MB/s · 0:03 left"; size only until the speed is known.</summary>
    private string TransferText()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        CultureInfo culture = localization.Culture;
        string size = _model.TotalBytes > 0
            ? TransferFormat.Progress(_model.DownloadedBytes, _model.TotalBytes, culture)
            : TransferFormat.Size(_model.DownloadedBytes, culture);

        if (_transferRateMeter.BytesPerSecond is not { } rate || rate <= 0)
            return size;

        string speed = TransferFormat.Size((long)rate, culture);
        if (_model.TotalBytes <= 0)
            return localization.Format("Progress_TransferNoTotal", size, speed);

        TimeSpan left = TimeSpan.FromSeconds(Math.Ceiling((_model.TotalBytes - _model.DownloadedBytes) / rate));
        return localization.Format("Progress_Transfer", size, speed, TransferFormat.Duration(left));
    }

    /// <summary>Order in which the item was added to the list; set by the main view model.</summary>
    public int Sequence { get; init; }

    /// <summary>Sort key for "by status": running first, then failed, waiting and finished.</summary>
    public int StatusSortOrder => _model.Status switch
    {
        DownloadStatus.Downloading => 0,
        DownloadStatus.WaitingToExtract or DownloadStatus.Extracting => 1,
        DownloadStatus.Failed or DownloadStatus.ExtractionFailed => 2,
        DownloadStatus.Queued => 3,
        _ => 4
    };

    [RelayCommand]
    private void CopyUrlToClipboard()
    {
        try
        {
            Clipboard.SetText(Url);
        }
        catch (COMException)
        {
            // Another application holds the clipboard open; nothing was copied.
            return;
        }

        LocalizationManager localization = LocalizationManager.Instance;
        _notificationService.Notify(
            localization.Get("App_Name"),
            localization.Format("Notify_UrlCopied", FileName),
            INotificationService.StatusTag);
    }
}
