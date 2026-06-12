using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Registers the application in the per-user "Run" registry key so it launches
/// automatically when the user logs in.
/// </summary>
public sealed class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TSRDownloader";

    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read startup registry value");
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (enabled)
            {
                string? executablePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(executablePath))
                {
                    _logger.LogWarning("Could not determine executable path; startup not enabled");
                    return;
                }

                key.SetValue(ValueName, $"\"{executablePath}\"");
                _logger.LogInformation("Enabled run on startup: {Path}", executablePath);
            }
            else
            {
                RemoveRegistration();
                _logger.LogInformation("Disabled run on startup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update startup registry value");
        }
    }

    /// <summary>
    /// Removes the "Run" registry value. Static so the uninstall hook can call it before the
    /// host (and logging) exists.
    /// </summary>
    public static void RemoveRegistration()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
