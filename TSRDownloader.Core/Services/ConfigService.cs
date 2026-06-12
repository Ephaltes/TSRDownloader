using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TSRDownloader.Core.Helpers;
using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Loads and saves the user's AppConfig to/from a JSON settings file.
/// Reads initial (default) values from IConfiguration (hosting pipeline).
/// </summary>
public class ConfigService : IConfigService
{
    // Enums are stored by name so the settings file stays readable (numbers are still accepted).
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _configPath;
    private readonly ILogger<ConfigService> _logger;

    public AppConfig Config { get; private set; } = new();

    public ConfigService(IConfiguration configuration, ILogger<ConfigService> logger, string? configPath = null)
    {
        _logger = logger;
        _configPath = configPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        // Bind from IConfiguration first
        AppConfig? boundConfig = configuration.GetSection("AppConfig").Get<AppConfig>();
        if (boundConfig is not null)
            Config = Normalize(boundConfig);

        _logger.LogDebug("Default configuration bound. Directory: {Directory}", Config.DownloadDirectory);
    }

    public void Load()
    {
        // Configuration is already loaded via IConfiguration in constructor.
        // This method reloads from the JSON file on disk.
        if (File.Exists(_configPath))
        {
            try
            {
                string json = File.ReadAllText(_configPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("AppConfig", out JsonElement appConfigElement))
                {
                    AppConfig? boundConfig = JsonSerializer.Deserialize<AppConfig>(
                        appConfigElement.GetRawText(), SerializerOptions);

                    if (boundConfig is not null)
                        Config = Normalize(boundConfig);
                }

                _logger.LogDebug("Configuration reloaded from {Path}", _configPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reload configuration, keeping current values");
            }
        }
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(new { AppConfig = Config }, SerializerOptions);
            AtomicFile.WriteAllText(_configPath, json);
            _logger.LogDebug("Configuration saved to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {Path}", _configPath);
        }
    }

    public void Update(AppConfig config)
    {
        Config = Normalize(config);
        Save();
    }

    /// <summary>
    /// Repairs values a hand-edited or outdated settings file may contain: an empty download
    /// directory falls back to the default, numbers are clamped to their allowed ranges.
    /// </summary>
    private static AppConfig Normalize(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DownloadDirectory))
            config.DownloadDirectory = AppConfig.DefaultDownloadDirectory;

        config.MaxActiveDownloads = Math.Clamp(config.MaxActiveDownloads,
            AppConfig.MinActiveDownloads, AppConfig.MaxActiveDownloadsLimit);
        config.TicketActivationDelaySeconds = Math.Clamp(config.TicketActivationDelaySeconds,
            AppConfig.MinTicketActivationDelaySeconds, AppConfig.MaxTicketActivationDelaySeconds);
        config.Language = string.IsNullOrWhiteSpace(config.Language) ? "en" : config.Language;

        return config;
    }
}
