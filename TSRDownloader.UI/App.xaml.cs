using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Services;
using TSRDownloader.UI.ViewModels;

namespace TSRDownloader.UI;

/// <summary>
/// Application entry point. Configures the generic host with Serilog logging,
/// dependency injection, and starts the main window.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;
    private readonly ILogger<App> _logger;

    public IServiceProvider Services => _host.Services;

    public App()
    {
        _host = BuildHost();
        _logger = _host.Services.GetRequiredService<ILogger<App>>();
    }

    private static IHost BuildHost()
    {
        // The shipped appsettings.json sits next to the exe, whatever the working directory is.
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        // User data (settings, download queue, logs) lives outside the install folder so it survives updates.
        AppPaths paths = AppPaths.FromVelopack();
        builder.Services.AddSingleton(paths);

        // Configure Serilog via Microsoft.Extensions.Logging (sinks come from appsettings.json)
        Log.Logger = LoggingSetup.CreateLogger(builder.Configuration, paths);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: true);

        // Core services
        builder.Services.AddSingleton<IConfigService>(serviceProvider => new ConfigService(
            serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
            serviceProvider.GetRequiredService<ILogger<ConfigService>>(),
            paths.SettingsFile));
        builder.Services.AddSingleton<IClipboardService, ClipboardService>();

        // TSR download client — owns the HttpClient and the TSR protocol. Cookies must be on:
        // the download ticket travels as a cookie between the protocol's requests.
        builder.Services.AddSingleton<ITsrDownloadClient>(serviceProvider =>
        {
            HttpClientHandler handler = new()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AllowAutoRedirect = true
            };
            HttpClient httpClient = new(handler);
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromMinutes(30);

            // Read per download, so a changed setting applies without a restart.
            IConfigService configService = serviceProvider.GetRequiredService<IConfigService>();
            return new TsrDownloadClient(
                httpClient,
                () => TimeSpan.FromSeconds(configService.Config.TicketActivationDelaySeconds));
        });

        // Archive extraction (Core) — single-worker sequential queue
        builder.Services.AddSingleton<IArchiveExtractor, ZipArchiveExtractor>();
        builder.Services.AddSingleton<IArchiveExtractionService, ArchiveExtractionService>();

        // Download service — queue orchestration on top of the TSR download client
        builder.Services.AddSingleton<IDownloadService, DownloadService>();

        // Download list persistence — saved on every change, restored at startup
        builder.Services.AddSingleton<IDownloadQueueStore>(serviceProvider => new DownloadQueueStore(
            paths.QueueFile, serviceProvider.GetRequiredService<ILogger<DownloadQueueStore>>()));
        builder.Services.AddSingleton<DownloadQueueAutosave>();

        // ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // UI-specific services
        builder.Services.AddSingleton<IUiDispatcher, WpfDispatcher>();
        builder.Services.AddSingleton<ClipboardHookService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<IStartupService, StartupService>();
        builder.Services.AddSingleton<IFileConflictResolver, FileConflictResolver>();
        builder.Services.AddSingleton(Localization.LocalizationManager.Instance);

        // Updates (Velopack / GitHub Releases)
        builder.Services.AddSingleton<IUpdateService, VelopackUpdateService>();
        builder.Services.AddSingleton<IUpdateWindowService, UpdateWindowService>();
        builder.Services.AddSingleton<UpdateCoordinator>();

        return builder.Build();
    }

    /// <summary>
    /// Logs every unhandled exception before the process dies (or, for unobserved tasks,
    /// instead of silently losing it), so crashes leave a trace in the log file.
    /// </summary>
    private void RegisterGlobalExceptionLogging()
    {
        DispatcherUnhandledException += (_, args) =>
            _logger.LogCritical(args.Exception, "Unhandled exception on the UI thread");
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            _logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
            Log.CloseAndFlush();
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger.LogError(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterGlobalExceptionLogging();

        await _host.StartAsync();

        // Load configuration
        IConfigService configService = Services.GetRequiredService<IConfigService>();
        configService.Load();

        // Apply the configured UI language before any window is shown
        Localization.LocalizationManager.Instance.SetLanguage(configService.Config.Language);

        // The update window may open and close before the main window exists; keep the app
        // alive until the main window takes over.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Automatic updates run before the main window and clipboard hook start, so no
        // download is interrupted. On success Velopack exits and restarts the app.
        UpdateCoordinator updateCoordinator = Services.GetRequiredService<UpdateCoordinator>();
        if (await updateCoordinator.RunStartupUpdateAsync())
        {
            // Velopack normally exits the process itself; never linger without a window.
            Shutdown();
            return;
        }

        // Ensure download directory exists
        try
        {
            Directory.CreateDirectory(configService.Config.DownloadDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create download directory {Directory}, using default",
                configService.Config.DownloadDirectory);

            configService.Config.DownloadDirectory = Core.Models.AppConfig.DefaultDownloadDirectory;
            Directory.CreateDirectory(configService.Config.DownloadDirectory);
        }

        _logger.LogInformation("TSR Downloader starting. Download directory: {Directory}",
            configService.Config.DownloadDirectory);

        // Bring back the previous run's downloads; they continue once monitoring starts.
        // The autosave is resolved first so it records every change from here on.
        Services.GetRequiredService<DownloadQueueAutosave>();
        MainViewModel mainViewModel = Services.GetRequiredService<MainViewModel>();
        mainViewModel.RestoreDownloads(Services.GetRequiredService<IDownloadQueueStore>().Load());

        // Launch main window
        MainWindow mainWindow = new(mainViewModel, Services.GetRequiredService<ClipboardHookService>());
        // Set explicitly: an update window created earlier would otherwise be Application.MainWindow.
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();

        // Notify-only mode checks in the background once the app is usable.
        _ = updateCoordinator.CheckAndNotifyAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger.LogInformation("TSR Downloader shutting down");

        // Synchronous on purpose: with an async void override the process could exit at the
        // first await, before Dispose saves the download queue and flushes the log.
        // No hosted services are registered and the host does not need the UI thread, so this cannot deadlock.
        _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        _host.Dispose();
        base.OnExit(e);
    }
}
