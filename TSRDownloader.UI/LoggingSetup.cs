using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI;

/// <summary>
/// Builds the Serilog logger from the "Serilog" configuration section. The file sink path in
/// appsettings.json refers to <c>%TSRDOWNLOADER_DATA%</c>, which is set here to the data
/// directory so logs survive updates.
/// </summary>
public static class LoggingSetup
{
    public static Logger CreateLogger(IConfiguration configuration, AppPaths paths)
    {
        Directory.CreateDirectory(paths.LogDirectory);
        Environment.SetEnvironmentVariable(AppPaths.DataDirectoryVariable, paths.DataDirectory);

        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}
