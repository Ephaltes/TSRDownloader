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
    private readonly IUpdateService _updateService;
    private readonly LocalizationManager _localization;
    private readonly IUiDispatcher _dispatcher;
    private readonly Dictionary<int, DownloadItemViewModel> _vmLookup = [];
    private int _nextSequence;

    [ObservableProperty]
    private ObservableCollection<DownloadItemViewModel> _downloadItems = [];

    /// <summary>The selected rows; set by the view, since a ListView's SelectedItems cannot be bound.</summary>
    public IReadOnlyList<DownloadItemViewModel> SelectedItems { get; private set; } = [];

    /// <summary>Whether copied TSR links are picked up from the clipboard.</summary>
    [ObservableProperty]
    private bool _isMonitoring;

    /// <summary>Whether queued items are downloaded (false while the user paused downloads).</summary>
    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;

    /// <summary>Failed items of the current batch (see <see cref="BatchItems"/>).</summary>
    [ObservableProperty]
    private int _failedCount;

    /// <summary>Failed items in the whole list.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetryFailedCommand))]
    private int _errorCount;

    /// <summary>Items waiting for or in extraction.</summary>
    [ObservableProperty]
    private int _extractingCount;

    private readonly HashSet<int> _batch = [];
    private bool _wasBusy;

    [ObservableProperty]
    private int _queuedCount;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private double _overallProgress;

    /// <summary>"Version 1.1.7", or "Development build" when not installed via the installer.</summary>
    [ObservableProperty]
    private string _versionText = string.Empty;

    public MainViewModel(
        IDownloadService downloadService,
        IArchiveExtractionService extractionService,
        IClipboardService clipboardService,
        IConfigService configService,
        INotificationService notificationService,
        IUpdateService updateService,
        LocalizationManager localization,
        IUiDispatcher dispatcher)
    {
        _downloadService = downloadService;
        _extractionService = extractionService;
        _clipboardService = clipboardService;
        _configService = configService;
        _notificationService = notificationService;
        _updateService = updateService;
        _localization = localization;
        _dispatcher = dispatcher;
        VersionText = FormatVersionText();

        DownloadItemsView = new ListCollectionView(DownloadItems)
        {
            Filter = MatchesSearch,
            IsLiveSorting = true,
            IsLiveFiltering = true
        };
        DownloadItemsView.LiveSortingProperties.Add(nameof(DownloadItemViewModel.StatusSortOrder));
        DownloadItemsView.LiveSortingProperties.Add(nameof(DownloadItemViewModel.FileName));
        DownloadItemsView.LiveFilteringProperties.Add(nameof(DownloadItemViewModel.FileName));
        SortOptions = BuildSortOptions();
        SelectedSort = SortOptions[0];

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
            VersionText = FormatVersionText();

            ListSort current = SelectedSort.Sort;
            SortOptions = BuildSortOptions();
            SelectedSort = SortOptions.First(option => option.Sort == current);
        });
    }

    /// <summary>The list as shown: filtered by <see cref="SearchText"/>, ordered by <see cref="SelectedSort"/>.</summary>
    public ListCollectionView DownloadItemsView { get; }

    /// <summary>Shows only items whose file name, link or id contains this text.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<SortOption> _sortOptions = [];

    [ObservableProperty]
    private SortOption _selectedSort = null!;

    private IReadOnlyList<SortOption> BuildSortOptions() =>
    [
        new(ListSort.OldestFirst, _localization.Get("Sort_OldestFirst")),
        new(ListSort.NewestFirst, _localization.Get("Sort_NewestFirst")),
        new(ListSort.Status, _localization.Get("Sort_Status")),
        new(ListSort.Name, _localization.Get("Sort_Name")),
    ];

    partial void OnSearchTextChanged(string value) => DownloadItemsView.Refresh();

    partial void OnSelectedSortChanged(SortOption value)
    {
        SortDescriptionCollection sorting = DownloadItemsView.SortDescriptions;
        using (DownloadItemsView.DeferRefresh())
        {
            sorting.Clear();
            // Oldest first is the list's own order, so it needs no sort description.
            switch (value.Sort)
            {
                case ListSort.NewestFirst:
                    sorting.Add(new SortDescription(nameof(DownloadItemViewModel.Sequence), ListSortDirection.Descending));
                    break;
                case ListSort.Status:
                    sorting.Add(new SortDescription(nameof(DownloadItemViewModel.StatusSortOrder), ListSortDirection.Ascending));
                    sorting.Add(new SortDescription(nameof(DownloadItemViewModel.Sequence), ListSortDirection.Ascending));
                    break;
                case ListSort.Name:
                    sorting.Add(new SortDescription(nameof(DownloadItemViewModel.FileName), ListSortDirection.Ascending));
                    sorting.Add(new SortDescription(nameof(DownloadItemViewModel.Sequence), ListSortDirection.Ascending));
                    break;
            }
        }
    }

    private bool MatchesSearch(object item)
    {
        string term = SearchText.Trim();
        if (term.Length == 0)
            return true;

        DownloadItemViewModel vm = (DownloadItemViewModel)item;
        return (vm.Model.FileName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
               || vm.Url.Contains(term, StringComparison.OrdinalIgnoreCase)
               || vm.ItemId.ToString(CultureInfo.InvariantCulture).Contains(term, StringComparison.Ordinal);
    }

    private string FormatVersionText() => _updateService.IsInstalled
        ? _localization.Format("Settings_Version", _updateService.CurrentVersion ?? string.Empty)
        : _localization.Get("Settings_DevelopmentBuild");

    /// <summary>
    /// Called by the ClipboardHookService when new clipboard content is detected.
    /// Passes the raw text to the clipboard service for parsing.
    /// </summary>
    public void OnClipboardContentChanged(string clipboardText)
    {
        if (!IsMonitoring) return;

        int added = AddLinks(clipboardText);
        if (added == 0 || !_configService.Config.NotifyOnLinkFound) return;

        string message = added == 1
            ? _localization.Get("Notify_LinkFound_One")
            : _localization.Format("Notify_LinkFound_Many", added);
        _notificationService.Notify(_localization.Get("App_Name"), message);
    }

    /// <summary>
    /// Queues the new TSR links found in <paramref name="text"/> (pasted or dropped by the user,
    /// or copied to the clipboard). Works while clipboard watching is paused.
    /// Returns how many links were added.
    /// </summary>
    public int AddLinks(string text)
    {
        List<DownloadItem> newItems = _clipboardService.ProcessClipboardContent(text);
        if (newItems.Count == 0) return 0;

        AddToList(newItems);
        _downloadService.EnqueueRange(newItems);
        return newItems.Count;
    }

    /// <summary>Adds the links currently on the clipboard (Ctrl+V).</summary>
    [RelayCommand]
    private void PasteLinks()
    {
        string text;
        try { text = Clipboard.GetText(); }
        catch (System.Runtime.InteropServices.ExternalException) { return; } // clipboard held by another app

        AddLinks(text);
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
            DownloadItemViewModel vm = new(item, _notificationService) { Sequence = _nextSequence++ };
            _vmLookup[item.ItemId] = vm;
            DownloadItems.Add(vm);
        }
    }

    /// <summary>Starts watching the clipboard and downloading (on startup).</summary>
    [RelayCommand]
    private void StartMonitoring()
    {
        IsMonitoring = true;
        StartDownloads();
    }

    /// <summary>Stops watching the clipboard and downloading (on shutdown).</summary>
    [RelayCommand]
    private void StopMonitoring()
    {
        IsMonitoring = false;
        StopDownloads();
    }

    /// <summary>Pauses or resumes picking up links from the clipboard; downloads are unaffected.</summary>
    [RelayCommand]
    private void ToggleClipboard() => IsMonitoring = !IsMonitoring;

    /// <summary>Pauses or resumes all downloads; new links are still added to the list.</summary>
    [RelayCommand]
    private void ToggleDownloads()
    {
        if (IsDownloading)
            StopDownloads();
        else
            StartDownloads();
    }

    private void StartDownloads()
    {
        IsDownloading = true;
        _downloadService.Start();
    }

    private void StopDownloads()
    {
        IsDownloading = false;
        _downloadService.Stop();
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

    /// <summary>Called by the view whenever the selection changes.</summary>
    public void SetSelection(IEnumerable<DownloadItemViewModel> selectedItems)
    {
        SelectedItems = selectedItems.ToList();
        NotifySelectionCommandsChanged();
    }

    private void NotifySelectionCommandsChanged()
    {
        RetryCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        ShowInFolderCommand.NotifyCanExecuteChanged();
        OpenCommand.NotifyCanExecuteChanged();
        OpenOnTsrCommand.NotifyCanExecuteChanged();
        CopyUrlsCommand.NotifyCanExecuteChanged();
    }

    private static bool IsFailed(DownloadItemViewModel vm) => IsFailed(vm.Model);

    // Items owned by the extraction queue cannot be removed until extraction has finished.
    private static bool IsRemovable(DownloadItemViewModel vm) =>
        vm.Model.Status is not (DownloadStatus.WaitingToExtract or DownloadStatus.Extracting);

    /// <summary>Re-queues the selected failed items.</summary>
    [RelayCommand(CanExecute = nameof(CanRetry))]
    private void Retry()
    {
        foreach (DownloadItemViewModel vm in SelectedItems.Where(IsFailed).ToList())
            _downloadService.Retry(vm.Model);
    }

    private bool CanRetry() => SelectedItems.Any(IsFailed);

    /// <summary>Re-queues every failed item.</summary>
    [RelayCommand(CanExecute = nameof(CanRetryFailed))]
    private void RetryFailed()
    {
        foreach (DownloadItem item in _downloadService.Items.Where(IsFailed))
            _downloadService.Retry(item);
    }

    private bool CanRetryFailed() => ErrorCount > 0;

    /// <summary>Removes the selected items that are not being extracted.</summary>
    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        foreach (DownloadItemViewModel vm in SelectedItems.Where(IsRemovable).ToList())
            RemoveItem(vm);
    }

    private bool CanRemove() => SelectedItems.Any(IsRemovable);

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
        SetSelection(SelectedItems.Where(selected => selected != vm));
        // Copying the link again should add it again.
        _clipboardService.Forget(vm.ItemId);
    }

    /// <summary>The first selected item that has been downloaded (folder actions open one window only).</summary>
    private DownloadItem? FirstDownloadedSelection =>
        SelectedItems.Select(vm => vm.Model).FirstOrDefault(item => item.FilePath is not null);

    private static string ExtractedFolder(string filePath) =>
        Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileNameWithoutExtension(filePath));

    [RelayCommand(CanExecute = nameof(CanShowInFolder))]
    private void ShowInFolder()
    {
        string? filePath = FirstDownloadedSelection?.FilePath;
        if (filePath is null)
            return;

        // After extraction the archive may be gone; select the extracted folder instead.
        string extractedFolder = ExtractedFolder(filePath);
        string target = File.Exists(filePath) ? filePath
            : Directory.Exists(extractedFolder) ? extractedFolder
            : Path.GetDirectoryName(filePath) ?? string.Empty;

        StartExplorer($"/select,\"{target}\"");
    }

    private bool CanShowInFolder() => FirstDownloadedSelection is not null;

    /// <summary>
    /// Opens the extracted folder, or a .zip that was not extracted (Explorer shows its content).
    /// Other downloaded files are never started from here.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        if (OpenTarget() is { } target)
            StartExplorer($"\"{target}\"");
    }

    private bool CanOpen() => OpenTarget() is not null;

    private string? OpenTarget()
    {
        string? filePath = FirstDownloadedSelection?.FilePath;
        if (filePath is null)
            return null;

        string extractedFolder = ExtractedFolder(filePath);
        if (Directory.Exists(extractedFolder))
            return extractedFolder;
        return File.Exists(filePath) && Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase)
            ? filePath
            : null;
    }

    /// <summary>Opens the TSR page of each selected item in the browser.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenOnTsr()
    {
        foreach (DownloadItemViewModel vm in SelectedItems)
        {
            try
            {
                Process.Start(new ProcessStartInfo(TsrPageUrl(vm.Model)) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // No browser registered — nothing sensible to do.
            }
        }
    }

    /// <summary>
    /// The copied link if it is a real http(s) TSR address, otherwise a link built from the item id.
    /// Never hands arbitrary clipboard text to the shell.
    /// </summary>
    public static string TsrPageUrl(DownloadItem item) =>
        Uri.TryCreate(item.Url, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && (uri.Host.Equals("thesimsresource.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".thesimsresource.com", StringComparison.OrdinalIgnoreCase))
            ? uri.AbsoluteUri
            : $"https://www.thesimsresource.com/downloads/{item.ItemId}";

    /// <summary>Copies the links of the selected items, one per line.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void CopyUrls()
    {
        if (SelectedItems.Count == 1)
        {
            SelectedItems[0].CopyUrlToClipboardCommand.Execute(null);
            return;
        }

        try
        {
            Clipboard.SetText(string.Join(Environment.NewLine, SelectedItems.Select(vm => vm.Url)));
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            return; // another application holds the clipboard open
        }

        _notificationService.Notify(_localization.Get("App_Name"),
            _localization.Format("Notify_UrlsCopied", SelectedItems.Count));
    }

    private bool HasSelection() => SelectedItems.Count > 0;

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
            if (SelectedItems.Contains(vm))
                NotifySelectionCommandsChanged();

            // Keeps the overall bar moving with the running downloads' byte progress.
            if (item.Status == DownloadStatus.Downloading)
                UpdateOverallProgress(_downloadService.Items);
        });
    }

    private static bool IsFinished(DownloadItem item) => item.Status is DownloadStatus.Completed
        or DownloadStatus.ExtractionCompleted or DownloadStatus.Failed or DownloadStatus.ExtractionFailed;

    private static bool IsFailed(DownloadItem item) =>
        item.Status is DownloadStatus.Failed or DownloadStatus.ExtractionFailed;

    /// <summary>
    /// The items of the current batch: everything that was pending at some point since the queue
    /// last became busy. Empty when nothing has been pending since startup; then all items count.
    /// </summary>
    private List<DownloadItem> BatchItems(IReadOnlyList<DownloadItem> items) =>
        _batch.Count == 0 ? items.ToList() : items.Where(item => _batch.Contains(item.ItemId)).ToList();

    private void UpdateOverallProgress(IReadOnlyList<DownloadItem> items)
    {
        List<DownloadItem> batch = BatchItems(items);
        // A finished download counts fully once it reached extraction; a running one by its bytes.
        double done = batch.Sum(item => item.Status switch
        {
            DownloadStatus.Queued => 0,
            DownloadStatus.Downloading => item.Progress / 100,
            _ => 1
        });
        OverallProgress = batch.Count > 0 ? done / batch.Count * 100 : 0;
    }

    private void OnQueueStatsChanged()
    {
        _dispatcher.Invoke(() =>
        {
            IReadOnlyList<DownloadItem> items = _downloadService.Items;
            bool isBusy = items.Any(item => !IsFinished(item));

            // A new batch starts when work arrives on an idle queue; everything pending joins it.
            if (isBusy && !_wasBusy)
                _batch.Clear();
            foreach (DownloadItem item in items.Where(item => !IsFinished(item)))
                _batch.Add(item.ItemId);
            _batch.IntersectWith(items.Select(item => item.ItemId)); // drop removed items

            List<DownloadItem> batch = BatchItems(items);
            TotalCount = batch.Count;
            CompletedCount = batch.Count(item =>
                item.Status is DownloadStatus.Completed or DownloadStatus.ExtractionCompleted);
            FailedCount = batch.Count(IsFailed);
            ErrorCount = items.Count(IsFailed);
            QueuedCount = items.Count(downloadItem => downloadItem.Status == DownloadStatus.Queued);
            ActiveCount = items.Count(downloadItem => downloadItem.Status == DownloadStatus.Downloading);
            ExtractingCount = items.Count(downloadItem =>
                downloadItem.Status is DownloadStatus.WaitingToExtract or DownloadStatus.Extracting);
            UpdateOverallProgress(items);

            if (_wasBusy && !isBusy && CompletedCount + FailedCount > 0)
                NotifyQueueFinished();
            _wasBusy = isBusy;

            // Queued items start in the service's order, whatever order the list is shown in.
            int position = 0;
            foreach (DownloadItem item in items)
            {
                if (_vmLookup.TryGetValue(item.ItemId, out DownloadItemViewModel? vm))
                    vm.QueuePosition = item.Status == DownloadStatus.Queued ? ++position : 0;
            }
        });
    }

    private void NotifyQueueFinished()
    {
        string message = FailedCount == 0
            ? _localization.Format("Notify_QueueFinished", CompletedCount)
            : _localization.Format("Notify_QueueFinishedWithErrors", CompletedCount, FailedCount);
        _notificationService.Notify(_localization.Get("App_Name"), message);
    }
}
