using TSRDownloader.Core.Models;

namespace TSRDownloader.Core.Services;

/// <summary>
/// Service for managing and persisting application configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>Gets the current configuration.</summary>
    AppConfiguration Configuration { get; }

    /// <summary>Loads configuration from disk.</summary>
    void Load();

    /// <summary>Saves the current configuration to disk.</summary>
    void Save();

    /// <summary>Updates and persists the configuration.</summary>
    void Update(AppConfiguration configuration);
}
