using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Serilog;
using Shouldly;
using TSRDownloader.TestHelper;
using TSRDownloader.UI;
using TSRDownloader.UI.Services;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class LoggingSetupTests
{
    [Fact]
    public void CreateLogger_Should_WriteToDataDirectory_When_SinkPathUsesDataVariable()
    {
        // Arrange
        using TempDir temp = new();
        AppPaths paths = new(installedRootDirectory: temp.Path, fallbackDirectory: temp.Path);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:WriteTo:0:Name"] = "File",
                ["Serilog:WriteTo:0:Args:path"] = "%TSRDOWNLOADER_DATA%/logs/test-.log",
                ["Serilog:WriteTo:0:Args:rollingInterval"] = "Day"
            })
            .Build();

        // Act
        using (Serilog.Core.Logger logger = LoggingSetup.CreateLogger(configuration, paths))
            logger.Information("hello");

        // Assert
        Directory.Exists(paths.LogDirectory).ShouldBeTrue();
        Directory.GetFiles(paths.LogDirectory, "test-*.log").Length.ShouldBe(1);
    }
}
