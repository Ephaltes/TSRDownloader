using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TSRDownloader.Core.Models;
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

    [ObservableProperty]
    private string _statusIcon = string.Empty;

    [ObservableProperty]
    private Brush _statusColor = Brushes.Gray;

    public DownloadItemViewModel(DownloadItem model, INotificationService notificationService)
    {
        _model = model;
        _notificationService = notificationService;
        SyncFromModel();
    }

    /// <summary>
    /// Synchronizes ViewModel properties from the underlying model.
    /// Called when the download service reports changes.
    /// </summary>
    public void SyncFromModel()
    {
        DownloadStatusPresentation presentation = DownloadStatusPresenter.For(_model.Status);

        StatusIcon = presentation.Icon;
        StatusText = LocalizationManager.Instance.Get(presentation.LabelKey);
        StatusColor = presentation.Color;
        IsIndeterminate = presentation.IsIndeterminate;

        Progress = _model.Status is DownloadStatus.Completed or DownloadStatus.ExtractionCompleted
            ? 100
            : _model.Progress;
        ProgressText = BuildProgressText();
    }

    private string BuildProgressText() => _model.Status switch
    {
        DownloadStatus.Queued => LocalizationManager.Instance.Get("Progress_QueuePosition"),
        DownloadStatus.Downloading => $"{_model.Progress:F1}%",
        DownloadStatus.Completed => "100%",
        DownloadStatus.Failed => _model.ErrorMessage ?? LocalizationManager.Instance.Get("Progress_UnknownError"),
        DownloadStatus.WaitingToExtract => LocalizationManager.Instance.Get("Progress_WaitingToExtract"),
        DownloadStatus.Extracting => $"{_model.Progress:F1}%",
        DownloadStatus.ExtractionCompleted => LocalizationManager.Instance.Get("Progress_ExtractionDone"),
        DownloadStatus.ExtractionFailed => _model.ErrorMessage ?? LocalizationManager.Instance.Get("Progress_ExtractionError"),
        _ => string.Empty
    };

    public void UpdateFromModel()
    {
        SyncFromModel();
        OnPropertyChanged(nameof(FileName));
    }

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

        LocalizationManager loc = LocalizationManager.Instance;
        _notificationService.Notify(
            loc.Get("App_Name"),
            loc.Format("Notify_UrlCopied", FileName));
    }
}
