using System.Linq;
using NSubstitute;
using Shouldly;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.UI.Localization;
using TSRDownloader.UI.Services;
using TSRDownloader.UI.ViewModels;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class SettingsViewModelTests
{
    private static (SettingsViewModel ViewModel, IConfigurationService ConfigurationService) CreateViewModel(
        AppConfiguration configuration, bool isInstalled = false, string? currentVersion = null, IThemeService? theme = null)
    {
        IConfigurationService configurationService = Substitute.For<IConfigurationService>();
        configurationService.Configuration.Returns(configuration);
        IStartupService startup = Substitute.For<IStartupService>();
        startup.IsEnabled().Returns(configuration.StartOnSystemStartup);
        IUpdateService updates = Substitute.For<IUpdateService>();
        updates.IsInstalled.Returns(isInstalled);
        updates.CurrentVersion.Returns(currentVersion);
        LocalizationManager.Instance.SetLanguage("en");
        SettingsViewModel viewModel = new(configurationService, startup, updates,
            theme ?? Substitute.For<IThemeService>(), LocalizationManager.Instance);
        return (viewModel, configurationService);
    }

    [Fact]
    public void Save_Should_KeepTheWindowLayout_When_Executed()
    {
        // Arrange
        WindowLayout layout = new() { Width = 1000, Height = 600 };
        (SettingsViewModel viewModel, IConfigurationService configurationService) = CreateViewModel(new AppConfiguration { MainWindowLayout = layout });

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        configurationService.Received(1).Update(Arg.Is<AppConfiguration>(saved => saved != null && saved.MainWindowLayout == layout));
    }

    [Fact]
    public void Save_Should_PersistCloseToTray_When_Changed()
    {
        // Arrange
        (SettingsViewModel viewModel, IConfigurationService configurationService) = CreateViewModel(new AppConfiguration { CloseToTray = true });
        viewModel.CloseToTray.ShouldBeTrue();

        // Act
        viewModel.CloseToTray = false;
        viewModel.SaveCommand.Execute(null);

        // Assert
        configurationService.Received(1).Update(Arg.Is<AppConfiguration>(saved => saved != null && !saved.CloseToTray));
    }

    [Fact]
    public void Save_Should_PersistAndApplyTheme_When_ThemeChanged()
    {
        // Arrange
        IThemeService theme = Substitute.For<IThemeService>();
        (SettingsViewModel viewModel, IConfigurationService configurationService) = CreateViewModel(new AppConfiguration { Theme = AppTheme.Light }, theme: theme);
        viewModel.SelectedTheme.Theme.ShouldBe(AppTheme.Light);

        // Act
        viewModel.SelectedTheme = viewModel.Themes.Single(option => option.Theme == AppTheme.Dark);
        viewModel.SaveCommand.Execute(null);

        // Assert
        configurationService.Received(1).Update(Arg.Is<AppConfiguration>(saved => saved != null && saved.Theme == AppTheme.Dark));
        theme.Received(1).Apply(AppTheme.Dark);
    }

    [Fact]
    public void LoadFromConfiguration_Should_PopulateExtractionSettings_When_Constructed()
    {
        // Arrange / Act
        (SettingsViewModel viewModel, _) = CreateViewModel(new AppConfiguration
        {
            ExtractZipArchives = false,
            DeleteZipAfterExtraction = false
        });

        // Assert
        viewModel.ExtractZipArchives.ShouldBeFalse();
        viewModel.DeleteZipAfterExtraction.ShouldBeFalse();
    }

    [Fact]
    public void Save_Should_PersistConfigurationWithClampedMax_When_Executed()
    {
        // Arrange
        (SettingsViewModel viewModel, IConfigurationService configurationService) = CreateViewModel(new AppConfiguration());
        viewModel.MaxActiveDownloads = 99;
        viewModel.ExtractZipArchives = true;
        viewModel.DeleteZipAfterExtraction = false;
        bool closed = false;
        viewModel.CloseRequested += () => closed = true;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        configurationService.Received(1).Update(Arg.Is<AppConfiguration>(saved => saved != null &&
            saved.MaxActiveDownloads == 10 &&
            saved.ExtractZipArchives &&
            !saved.DeleteZipAfterExtraction));
        closed.ShouldBeTrue();
    }

    [Fact]
    public void LoadFromConfiguration_Should_SelectConfiguredUpdateMode_When_Constructed()
    {
        // Arrange / Act
        (SettingsViewModel viewModel, _) = CreateViewModel(new AppConfiguration { UpdateMode = UpdateMode.NotifyOnly });

        // Assert
        viewModel.SelectedUpdateMode.Mode.ShouldBe(UpdateMode.NotifyOnly);
        viewModel.UpdateModes.Select(option => option.DisplayName)
            .ShouldBe(["Install automatically", "Notify only", "Off"]);
    }

    [Fact]
    public void Save_Should_PersistSelectedUpdateMode_When_Executed()
    {
        // Arrange
        (SettingsViewModel viewModel, IConfigurationService configurationService) = CreateViewModel(new AppConfiguration());
        viewModel.SelectedUpdateMode = viewModel.UpdateModes.Single(option => option.Mode == UpdateMode.Disabled);

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        configurationService.Received(1).Update(Arg.Is<AppConfiguration>(saved => saved!.UpdateMode == UpdateMode.Disabled));
    }

    [Fact]
    public void LoadFromConfiguration_Should_PopulateTicketActivationDelay_When_Constructed()
    {
        // Arrange / Act
        (SettingsViewModel viewModel, _) = CreateViewModel(new AppConfiguration { TicketActivationDelaySeconds = 12 });

        // Assert
        viewModel.TicketActivationDelaySeconds.ShouldBe(12);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(-3, 0)]
    [InlineData(500, 60)]
    public void Save_Should_PersistClampedTicketActivationDelay_When_Executed(int entered, int expected)
    {
        // Arrange
        (SettingsViewModel viewModel, IConfigurationService configurationService) = CreateViewModel(new AppConfiguration());
        viewModel.TicketActivationDelaySeconds = entered;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        configurationService.Received(1).Update(Arg.Is<AppConfiguration>(saved => saved!.TicketActivationDelaySeconds == expected));
        viewModel.TicketActivationDelaySeconds.ShouldBe(expected);
    }

    [Fact]
    public void VersionText_Should_ShowInstalledVersion_When_Installed()
    {
        // Arrange / Act
        (SettingsViewModel viewModel, _) = CreateViewModel(new AppConfiguration(), isInstalled: true, currentVersion: "1.1.7");

        // Assert
        viewModel.VersionText.ShouldBe("Version 1.1.7");
    }

    [Fact]
    public void VersionText_Should_ShowDevelopmentBuild_When_NotInstalled()
    {
        // Arrange / Act
        (SettingsViewModel viewModel, _) = CreateViewModel(new AppConfiguration(), isInstalled: false);

        // Assert
        viewModel.VersionText.ShouldBe("Development build");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"relative\folder")]
    public void Save_Should_RejectAndStayOpen_When_DownloadDirectoryIsInvalid(string directory)
    {
        // Arrange
        (SettingsViewModel viewModel, IConfigurationService configurationService) = CreateViewModel(new AppConfiguration());
        viewModel.DownloadDirectory = directory;
        bool closed = false;
        viewModel.CloseRequested += () => closed = true;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        configurationService.DidNotReceive().Update(Arg.Any<AppConfiguration>());
        viewModel.DownloadDirectoryError.ShouldNotBeNullOrEmpty();
        closed.ShouldBeFalse();
    }
}
