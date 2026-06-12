using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Service for managing and persisting application configuration.
/// </summary>
public interface IConfigService
{
    /// <summary>Gets the current configuration.</summary>
    AppConfig Config { get; }

    /// <summary>Loads configuration from disk.</summary>
    void Load();

    /// <summary>Saves the current configuration to disk.</summary>
    void Save();

    /// <summary>Updates and persists the configuration.</summary>
    void Update(AppConfig config);
}
