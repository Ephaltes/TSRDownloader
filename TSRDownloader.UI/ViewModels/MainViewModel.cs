using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI.ViewModels;

/// <summary>
/// Main ViewModel for the TSR Downloader application.
/// Manages the download list, clipboard monitoring, and overall state.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;
    private readonly IArchiveExtractionService _extractionService;
    private readonly IClipboardService _clipboardService;
    private readonly IConfigService _configService;
    private readonly INotificationService _notificationService;
    private readonly LocalizationManager _localization;
    private readonly IUiDispatcher _dispatcher;
    private readonly Dictionary<int, DownloadItemViewModel> _vmLookup = [];

    [ObservableProperty]
    private ObservableCollection<DownloadItemViewModel> _downloadItems = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetryCommand), nameof(RemoveCommand), nameof(ShowInFolderCommand))]
    private DownloadItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private int _queuedCount;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private double _overallProgress;

    public MainViewModel(
        IDownloadService downloadService,
        IArchiveExtractionService extractionService,
        IClipboardService clipboardService,
        IConfigService configService,
        INotificationService notificationService,
        LocalizationManager localization,
        IUiDispatcher dispatcher)
    {
        _downloadService = downloadService;
        _extractionService = extractionService;
        _clipboardService = clipboardService;
        _configService = configService;
        _notificationService = notificationService;
        _localization = localization;
        _dispatcher = dispatcher;

        _downloadService.ItemChanged += OnItemChanged;
        _downloadService.QueueStatsChanged += OnQueueStatsChanged;
        _extractionService.ItemChanged += OnItemChanged;
        _extractionService.StatsChanged += OnQueueStatsChanged;
        _localization.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        // Refresh the language-dependent text of every already-listed item.
        _dispatcher.Invoke(() =>
        {
            foreach (DownloadItemViewModel vm in DownloadItems)
                vm.UpdateFromModel();
        });
    }

    /// <summary>
    /// Called by the ClipboardHookService when new clipboard content is detected.
    /// Passes the raw text to the clipboard service for parsing.
    /// </summary>
    public void OnClipboardContentChanged(string clipboardText)
    {
        if (!IsMonitoring) return;

        List<DownloadItem> newItems = _clipboardService.ProcessClipboardContent(clipboardText);
        if (newItems.Count == 0) return;

        AddToList(newItems);
        _downloadService.EnqueueRange(newItems);

        if (_configService.Config.NotifyOnLinkFound)
        {
            string message = newItems.Count == 1
                ? _localization.Get("Notify_LinkFound_One")
                : _localization.Format("Notify_LinkFound_Many", newItems.Count);
            _notificationService.Notify(_localization.Get("App_Name"), message);
        }
    }

    /// <summary>
    /// Shows the downloads saved by the previous run and hands them back to the download
    /// service, which continues unfinished ones once monitoring starts.
    /// </summary>
    public void RestoreDownloads(IReadOnlyList<DownloadItem> savedItems)
    {
        List<DownloadItem> items = savedItems
            .Where(item => !_vmLookup.ContainsKey(item.ItemId))
            .DistinctBy(item => item.ItemId)
            .ToList();
        if (items.Count == 0) return;

        foreach (DownloadItem item in items)
            _clipboardService.MarkSeen(item.ItemId);

        AddToList(items);
        _downloadService.Restore(items);
    }

    private void AddToList(IEnumerable<DownloadItem> items)
    {
        foreach (DownloadItem item in items)
        {
            DownloadItemViewModel vm = new(item, _notificationService);
            _vmLookup[item.ItemId] = vm;
            DownloadItems.Add(vm);
        }
    }

    [RelayCommand]
    private void StartMonitoring()
    {
        IsMonitoring = true;
        _downloadService.Start();
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        IsMonitoring = false;
        _downloadService.Stop();
    }

    /// <summary>Pauses (stops monitoring and downloads) or resumes.</summary>
    [RelayCommand]
    private void ToggleMonitoring()
    {
        if (IsMonitoring)
            StopMonitoring();
        else
            StartMonitoring();
    }

    // Closing the main window (rather than Application.Shutdown) lets it ask for confirmation.
    [RelayCommand]
    private static void Exit() => System.Windows.Application.Current.MainWindow?.Close();

    [RelayCommand]
    private void OpenSettings()
    {
        Views.SettingsWindow settingsWindow = new()
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        settingsWindow.ShowDialog();

        _downloadService.SetMaxActiveDownloads(_configService.Config.MaxActiveDownloads);
    }

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private void Retry()
    {
        if (SelectedItem is not null)
            _downloadService.Retry(SelectedItem.Model);
    }

    private bool CanRetry() =>
        SelectedItem?.Model.Status is DownloadStatus.Failed or DownloadStatus.ExtractionFailed;

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedItem is not null)
            RemoveItem(SelectedItem);
    }

    // Items owned by the extraction queue cannot be removed until extraction has finished.
    private bool CanRemove() =>
        SelectedItem?.Model.Status is not (null or DownloadStatus.WaitingToExtract or DownloadStatus.Extracting);

    /// <summary>Removes every successfully finished item from the list.</summary>
    [RelayCommand]
    private void ClearFinished()
    {
        foreach (DownloadItemViewModel vm in DownloadItems
                     .Where(vm => vm.Model.Status is DownloadStatus.Completed or DownloadStatus.ExtractionCompleted)
                     .ToList())
            RemoveItem(vm);
    }

    private void RemoveItem(DownloadItemViewModel vm)
    {
        if (!_downloadService.Remove(vm.Model))
            return;

        DownloadItems.Remove(vm);
        _vmLookup.Remove(vm.ItemId);
        // Copying the link again should add it again.
        _clipboardService.Forget(vm.ItemId);
    }

    [RelayCommand(CanExecute = nameof(CanShowInFolder))]
    private void ShowInFolder()
    {
        string? filePath = SelectedItem?.Model.FilePath;
        if (filePath is null)
            return;

        // After extraction the archive may be gone; select the extracted folder instead.
        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        string extractedFolder = Path.Combine(directory, Path.GetFileNameWithoutExtension(filePath));
        string target = File.Exists(filePath) ? filePath
            : Directory.Exists(extractedFolder) ? extractedFolder
            : directory;

        StartExplorer($"/select,\"{target}\"");
    }

    private bool CanShowInFolder() => SelectedItem?.Model.FilePath is not null;

    [RelayCommand]
    private void OpenDownloadFolder()
    {
        string directory = _configService.Config.DownloadDirectory;
        try { Directory.CreateDirectory(directory); }
        catch (Exception) { return; }

        StartExplorer($"\"{directory}\"");
    }

    private static void StartExplorer(string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Explorer unavailable — nothing sensible to do.
        }
    }

    private void OnItemChanged(DownloadItem item)
    {
        _dispatcher.Invoke(() =>
        {
            if (!_vmLookup.TryGetValue(item.ItemId, out DownloadItemViewModel? vm))
                return;

            vm.UpdateFromModel();
            if (ReferenceEquals(vm, SelectedItem))
            {
                RetryCommand.NotifyCanExecuteChanged();
                RemoveCommand.NotifyCanExecuteChanged();
                ShowInFolderCommand.NotifyCanExecuteChanged();
            }
        });
    }

    private void OnQueueStatsChanged()
    {
        _dispatcher.Invoke(() =>
        {
            IReadOnlyList<DownloadItem> items = _downloadService.Items;
            TotalCount = items.Count;
            CompletedCount = items.Count(downloadItem =>
                downloadItem.Status is DownloadStatus.Completed or DownloadStatus.ExtractionCompleted);
            FailedCount = items.Count(downloadItem =>
                downloadItem.Status is DownloadStatus.Failed or DownloadStatus.ExtractionFailed);
            QueuedCount = items.Count(downloadItem => downloadItem.Status == DownloadStatus.Queued);
            ActiveCount = items.Count(downloadItem => downloadItem.Status == DownloadStatus.Downloading);

            OverallProgress = TotalCount > 0
                ? (double)(CompletedCount + FailedCount) / TotalCount * 100
                : 0;
        });
    }
}
