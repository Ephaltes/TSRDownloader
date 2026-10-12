using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Presentation;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly IStartupService _startupService;
    private readonly IThemeService _themeService;
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
    private bool _closeToTray = true;

    [ObservableProperty]
    private bool _extractZipArchives = true;

    [ObservableProperty]
    private bool _deleteZipAfterExtraction = true;

    [ObservableProperty]
    private LanguageOption _selectedLanguage = null!;

    [ObservableProperty]
    private UpdateModeOption _selectedUpdateMode = null!;

    [ObservableProperty]
    private ThemeOption _selectedTheme = null!;

    /// <summary>The themes the user can choose from (localized).</summary>
    public IReadOnlyList<ThemeOption> Themes { get; }

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
        IConfigurationService configurationService,
        IStartupService startupService,
        IUpdateService updateService,
        IThemeService themeService,
        LocalizationManager localization)
    {
        _configurationService = configurationService;
        _startupService = startupService;
        _themeService = themeService;
        _localization = localization;
        Themes =
        [
            new(AppTheme.Light, localization.Get("Settings_Theme_Light")),
            new(AppTheme.Dark, localization.Get("Settings_Theme_Dark")),
        ];
        UpdateModes =
        [
            new(UpdateMode.Automatic, localization.Get("Settings_UpdateMode_Automatic")),
            new(UpdateMode.NotifyOnly, localization.Get("Settings_UpdateMode_NotifyOnly")),
            new(UpdateMode.Disabled, localization.Get("Settings_UpdateMode_Disabled")),
        ];
        VersionText = AppVersionText.Format(updateService, localization);
        LoadFromConfiguration();
    }

    private void LoadFromConfiguration()
    {
        AppConfiguration configuration = _configurationService.Configuration;
        DownloadDirectory = configuration.DownloadDirectory;
        MaxActiveDownloads = configuration.MaxActiveDownloads;
        TicketActivationDelaySeconds = configuration.TicketActivationDelaySeconds;
        NotifyOnLinkFound = configuration.NotifyOnLinkFound;
        CloseToTray = configuration.CloseToTray;
        ExtractZipArchives = configuration.ExtractZipArchives;
        DeleteZipAfterExtraction = configuration.DeleteZipAfterExtraction;
        SelectedUpdateMode = UpdateModes.FirstOrDefault(option => option.Mode == configuration.UpdateMode)
                             ?? UpdateModes[0];
        SelectedLanguage = Languages.FirstOrDefault(language => language.Code == configuration.Language)
                           ?? Languages[0];
        SelectedTheme = Themes.FirstOrDefault(option => option.Theme == configuration.Theme) ?? Themes[0];
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
        MaxActiveDownloads = Math.Clamp(MaxActiveDownloads,
            AppConfiguration.MinActiveDownloads, AppConfiguration.MaxActiveDownloadsLimit);
        TicketActivationDelaySeconds = Math.Clamp(TicketActivationDelaySeconds,
            AppConfiguration.MinTicketActivationDelaySeconds, AppConfiguration.MaxTicketActivationDelaySeconds);

        // Copy the current configuration so values this dialog does not edit (window layout) survive.
        AppConfiguration configuration = _configurationService.Configuration with
        {
            DownloadDirectory = DownloadDirectory,
            MaxActiveDownloads = MaxActiveDownloads,
            TicketActivationDelaySeconds = TicketActivationDelaySeconds,
            Language = SelectedLanguage.Code,
            NotifyOnLinkFound = NotifyOnLinkFound,
            StartOnSystemStartup = StartOnSystemStartup,
            CloseToTray = CloseToTray,
            ExtractZipArchives = ExtractZipArchives,
            DeleteZipAfterExtraction = DeleteZipAfterExtraction,
            UpdateMode = SelectedUpdateMode.Mode,
            Theme = SelectedTheme.Theme
        };

        _configurationService.Update(configuration);
        _startupService.SetEnabled(StartOnSystemStartup);
        _localization.SetLanguage(SelectedLanguage.Code);
        _themeService.Apply(SelectedTheme.Theme);
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
        LoadFromConfiguration();
        CloseRequested?.Invoke();
    }

    /// <summary>Event raised when the window should be closed.</summary>
    public event Action? CloseRequested;
}
