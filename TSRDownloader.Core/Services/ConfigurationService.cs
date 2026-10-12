using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TSRDownloader.Core.Helpers;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Loads and saves the user's <see cref="AppConfiguration"/> to/from a JSON settings file.
/// Reads initial (default) values from <see cref="IConfiguration"/> (hosting pipeline).
/// </summary>
public class ConfigurationService : IConfigurationService
{
    /// <summary>
    /// Name of the settings section in appsettings.json and settings.json. Kept as "AppConfig"
    /// so settings files written by earlier versions still load.
    /// </summary>
    private const string SectionName = "AppConfig";

    // Enums are stored by name so the settings file stays readable (numbers are still accepted).
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _configurationPath;
    private readonly ILogger<ConfigurationService> _logger;

    public AppConfiguration Configuration { get; private set; } = new();

    public ConfigurationService(IConfiguration hostConfiguration, ILogger<ConfigurationService> logger, string? configurationPath = null)
    {
        _logger = logger;
        _configurationPath = configurationPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        AppConfiguration? defaults = hostConfiguration.GetSection(SectionName).Get<AppConfiguration>();
        if (defaults is not null)
            Configuration = Normalize(defaults);

        _logger.LogDebug("Default configuration bound. Directory: {Directory}", Configuration.DownloadDirectory);
    }

    /// <summary>Replaces the defaults bound in the constructor with the user's settings file, if one exists.</summary>
    public void Load()
    {
        if (!File.Exists(_configurationPath))
            return;

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(_configurationPath));
            if (document.RootElement.TryGetProperty(SectionName, out JsonElement section)
                && section.Deserialize<AppConfiguration>(SerializerOptions) is { } saved)
                Configuration = Normalize(saved);

            _logger.LogDebug("Configuration reloaded from {Path}", _configurationPath);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to reload configuration, keeping current values");
        }
    }

    public void Save()
    {
        try
        {
            Dictionary<string, AppConfiguration> file = new() { [SectionName] = Configuration };
            AtomicFile.WriteAllText(_configurationPath, JsonSerializer.Serialize(file, SerializerOptions));
            _logger.LogDebug("Configuration saved to {Path}", _configurationPath);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save configuration to {Path}", _configurationPath);
        }
    }

    public void Update(AppConfiguration configuration)
    {
        Configuration = Normalize(configuration);
        Save();
    }

    /// <summary>
    /// Repairs values a hand-edited or outdated settings file may contain: an empty download
    /// directory falls back to the default, numbers are clamped to their allowed ranges.
    /// </summary>
    private static AppConfiguration Normalize(AppConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.DownloadDirectory))
            configuration.DownloadDirectory = AppConfiguration.DefaultDownloadDirectory;

        configuration.MaxActiveDownloads = Math.Clamp(configuration.MaxActiveDownloads,
            AppConfiguration.MinActiveDownloads, AppConfiguration.MaxActiveDownloadsLimit);
        configuration.TicketActivationDelaySeconds = Math.Clamp(configuration.TicketActivationDelaySeconds,
            AppConfiguration.MinTicketActivationDelaySeconds, AppConfiguration.MaxTicketActivationDelaySeconds);
        configuration.Language = string.IsNullOrWhiteSpace(configuration.Language) ? "en" : configuration.Language;

        return configuration;
    }
}
