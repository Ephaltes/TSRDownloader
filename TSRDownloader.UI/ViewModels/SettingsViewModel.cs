using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    /// <summary>Allowed range (seconds) for the ticket activation delay.</summary>
    public const int MinTicketActivationDelaySeconds = AppConfig.MinTicketActivationDelaySeconds;
    public const int MaxTicketActivationDelaySeconds = AppConfig.MaxTicketActivationDelaySeconds;

    /// <summary>Allowed range for the number of parallel downloads.</summary>
    public const int MinActiveDownloads = AppConfig.MinActiveDownloads;
    public const int MaxActiveDownloadsLimit = AppConfig.MaxActiveDownloadsLimit;

    private readonly IConfigService _configService;
    private readonly IStartupService _startupService;
    private readonly LocalizationManager _localization;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDownloadDirectoryError))]
    private string _downloadDirectory = string.Empty;

    /// <summary>Why the entered download directory cannot be used; null when it is valid.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDownloadDirectoryError))]
    private string? _downloadDirectoryError;

    public bool HasDownloadDirectoryError => !string.IsNullOrEmpty(DownloadDirectoryError);

    [ObservableProperty]
    private int _maxActiveDownloads = 3;

    [ObservableProperty]
    private int _ticketActivationDelaySeconds = 8;

    [ObservableProperty]
    private bool _notifyOnLinkFound = true;

    [ObservableProperty]
    private bool _startOnSystemStartup;

    [ObservableProperty]
    private bool _extractZipArchives = true;

    [ObservableProperty]
    private bool _deleteZipAfterExtraction = true;

    [ObservableProperty]
    private LanguageOption _selectedLanguage = null!;

    [ObservableProperty]
    private UpdateModeOption _selectedUpdateMode = null!;

    /// <summary>The languages the user can choose from (shown by their own endonym).</summary>
    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("de", "Deutsch"),
        new("en", "English"),
    ];

    /// <summary>The update modes the user can choose from (localized).</summary>
    public IReadOnlyList<UpdateModeOption> UpdateModes { get; }

    /// <summary>"Version 1.1.7", or "Development build" when not installed via the installer.</summary>
    public string VersionText { get; }

    public SettingsViewModel(
        IConfigService configService,
        IStartupService startupService,
        IUpdateService updateService,
        LocalizationManager localization)
    {
        _configService = configService;
        _startupService = startupService;
        UpdateModes =
        [
            new(UpdateMode.Automatic, localization.Get("Settings_UpdateMode_Automatic")),
            new(UpdateMode.NotifyOnly, localization.Get("Settings_UpdateMode_NotifyOnly")),
            new(UpdateMode.Disabled, localization.Get("Settings_UpdateMode_Disabled")),
        ];
        VersionText = updateService.IsInstalled
            ? localization.Format("Settings_Version", updateService.CurrentVersion ?? string.Empty)
            : localization.Get("Settings_DevelopmentBuild");
        _localization = localization;
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        AppConfig config = _configService.Config;
        DownloadDirectory = config.DownloadDirectory;
        MaxActiveDownloads = config.MaxActiveDownloads;
        TicketActivationDelaySeconds = config.TicketActivationDelaySeconds;
        NotifyOnLinkFound = config.NotifyOnLinkFound;
        ExtractZipArchives = config.ExtractZipArchives;
        DeleteZipAfterExtraction = config.DeleteZipAfterExtraction;
        SelectedUpdateMode = UpdateModes.FirstOrDefault(option => option.Mode == config.UpdateMode)
                             ?? UpdateModes[0];
        SelectedLanguage = Languages.FirstOrDefault(language => language.Code == config.Language)
                           ?? Languages[0];
        DownloadDirectoryError = null;
        // The registry is the source of truth for the autostart state.
        StartOnSystemStartup = _startupService.IsEnabled();
    }

    [RelayCommand]
    private void BrowseDownloadDirectory()
    {
        Microsoft.Win32.OpenFolderDialog dialog = new()
        {
            Title = _localization.Get("Settings_ChooseDirectory"),
            InitialDirectory = DownloadDirectory,
            Multiselect = false
        };

        bool? result = dialog.ShowDialog();
        if (result == true && !string.IsNullOrEmpty(dialog.FolderName))
            DownloadDirectory = dialog.FolderName;
    }

    [RelayCommand]
    private void Save()
    {
        DownloadDirectory = DownloadDirectory.Trim();
        DownloadDirectoryError = ValidateDownloadDirectory(DownloadDirectory);
        if (DownloadDirectoryError is not null)
            return;

        // Clamp numeric settings to their valid ranges before saving
        int clampedDownloads = Math.Clamp(MaxActiveDownloads, MinActiveDownloads, MaxActiveDownloadsLimit);
        MaxActiveDownloads = clampedDownloads;
        TicketActivationDelaySeconds = Math.Clamp(TicketActivationDelaySeconds,
            MinTicketActivationDelaySeconds, MaxTicketActivationDelaySeconds);

        AppConfig config = new()
        {
            DownloadDirectory = DownloadDirectory,
            MaxActiveDownloads = clampedDownloads,
            TicketActivationDelaySeconds = TicketActivationDelaySeconds,
            Language = SelectedLanguage.Code,
            NotifyOnLinkFound = NotifyOnLinkFound,
            StartOnSystemStartup = StartOnSystemStartup,
            ExtractZipArchives = ExtractZipArchives,
            DeleteZipAfterExtraction = DeleteZipAfterExtraction,
            UpdateMode = SelectedUpdateMode.Mode
        };

        _configService.Update(config);
        _startupService.SetEnabled(StartOnSystemStartup);
        _localization.SetLanguage(SelectedLanguage.Code);
        CloseRequested?.Invoke();
    }

    // Editing the path clears a previous validation error; it is re-checked on save.
    partial void OnDownloadDirectoryChanged(string value) => DownloadDirectoryError = null;

    /// <summary>
    /// Returns a localized error if <paramref name="directory"/> cannot be a download folder
    /// (empty, relative or malformed); null when it is acceptable. The folder itself is created
    /// on the first download, so a not-yet-existing path is fine.
    /// </summary>
    private string? ValidateDownloadDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return _localization.Get("Settings_DownloadDirectoryRequired");

        bool valid = directory.IndexOfAny(Path.GetInvalidPathChars()) < 0
                     && Path.IsPathFullyQualified(directory);
        return valid ? null : _localization.Get("Settings_DownloadDirectoryInvalid");
    }

    [RelayCommand]
    private void Cancel()
    {
        LoadFromConfig();
        CloseRequested?.Invoke();
    }

    /// <summary>Event raised when the window should be closed.</summary>
    public event Action? CloseRequested;
}
