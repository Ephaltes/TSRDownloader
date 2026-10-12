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
    private static (SettingsViewModel Vm, IConfigService Config) NewVm(
        AppConfig config, bool isInstalled = false, string? currentVersion = null, IThemeService? theme = null)
    {
        IConfigService configService = Substitute.For<IConfigService>();
        configService.Config.Returns(config);
        IStartupService startup = Substitute.For<IStartupService>();
        startup.IsEnabled().Returns(config.StartOnSystemStartup);
        IUpdateService updates = Substitute.For<IUpdateService>();
        updates.IsInstalled.Returns(isInstalled);
        updates.CurrentVersion.Returns(currentVersion);
        LocalizationManager.Instance.SetLanguage("en");
        SettingsViewModel vm = new(configService, startup, updates,
            theme ?? Substitute.For<IThemeService>(), LocalizationManager.Instance);
        return (vm, configService);
    }

    [Fact]
    public void Save_Should_KeepTheWindowLayout_When_Executed()
    {
        // Arrange
        WindowLayout layout = new() { Width = 1000, Height = 600 };
        (SettingsViewModel vm, IConfigService config) = NewVm(new AppConfig { MainWindowLayout = layout });

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        config.Received(1).Update(Arg.Is<AppConfig>(c => c != null && c.MainWindowLayout == layout));
    }

    [Fact]
    public void Save_Should_PersistCloseToTray_When_Changed()
    {
        // Arrange
        (SettingsViewModel vm, IConfigService config) = NewVm(new AppConfig { CloseToTray = true });
        vm.CloseToTray.ShouldBeTrue();

        // Act
        vm.CloseToTray = false;
        vm.SaveCommand.Execute(null);

        // Assert
        config.Received(1).Update(Arg.Is<AppConfig>(c => c != null && !c.CloseToTray));
    }

    [Fact]
    public void Save_Should_PersistAndApplyTheme_When_ThemeChanged()
    {
        // Arrange
        IThemeService theme = Substitute.For<IThemeService>();
        (SettingsViewModel vm, IConfigService config) = NewVm(new AppConfig { Theme = AppTheme.Light }, theme: theme);
        vm.SelectedTheme.Theme.ShouldBe(AppTheme.Light);

        // Act
        vm.SelectedTheme = vm.Themes.Single(option => option.Theme == AppTheme.Dark);
        vm.SaveCommand.Execute(null);

        // Assert
        config.Received(1).Update(Arg.Is<AppConfig>(c => c != null && c.Theme == AppTheme.Dark));
        theme.Received(1).Apply(AppTheme.Dark);
    }

    [Fact]
    public void LoadFromConfig_Should_PopulateExtractionSettings_When_Constructed()
    {
        // Arrange / Act
        (SettingsViewModel vm, _) = NewVm(new AppConfig
        {
            ExtractZipArchives = false,
            DeleteZipAfterExtraction = false
        });

        // Assert
        vm.ExtractZipArchives.ShouldBeFalse();
        vm.DeleteZipAfterExtraction.ShouldBeFalse();
    }

    [Fact]
    public void Save_Should_PersistConfigWithClampedMax_When_Executed()
    {
        // Arrange
        (SettingsViewModel vm, IConfigService config) = NewVm(new AppConfig());
        vm.MaxActiveDownloads = 99;
        vm.ExtractZipArchives = true;
        vm.DeleteZipAfterExtraction = false;
        bool closed = false;
        vm.CloseRequested += () => closed = true;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        config.Received(1).Update(Arg.Is<AppConfig>(c => c != null &&
            c.MaxActiveDownloads == 10 &&
            c.ExtractZipArchives &&
            !c.DeleteZipAfterExtraction));
        closed.ShouldBeTrue();
    }

    [Fact]
    public void LoadFromConfig_Should_SelectConfiguredUpdateMode_When_Constructed()
    {
        // Arrange / Act
        (SettingsViewModel vm, _) = NewVm(new AppConfig { UpdateMode = UpdateMode.NotifyOnly });

        // Assert
        vm.SelectedUpdateMode.Mode.ShouldBe(UpdateMode.NotifyOnly);
        vm.UpdateModes.Select(option => option.DisplayName)
            .ShouldBe(["Install automatically", "Notify only", "Off"]);
    }

    [Fact]
    public void Save_Should_PersistSelectedUpdateMode_When_Executed()
    {
        // Arrange
        (SettingsViewModel vm, IConfigService config) = NewVm(new AppConfig());
        vm.SelectedUpdateMode = vm.UpdateModes.Single(option => option.Mode == UpdateMode.Disabled);

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        config.Received(1).Update(Arg.Is<AppConfig>(c => c!.UpdateMode == UpdateMode.Disabled));
    }

    [Fact]
    public void LoadFromConfig_Should_PopulateTicketActivationDelay_When_Constructed()
    {
        // Arrange / Act
        (SettingsViewModel vm, _) = NewVm(new AppConfig { TicketActivationDelaySeconds = 12 });

        // Assert
        vm.TicketActivationDelaySeconds.ShouldBe(12);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(-3, 0)]
    [InlineData(500, 60)]
    public void Save_Should_PersistClampedTicketActivationDelay_When_Executed(int entered, int expected)
    {
        // Arrange
        (SettingsViewModel vm, IConfigService config) = NewVm(new AppConfig());
        vm.TicketActivationDelaySeconds = entered;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        config.Received(1).Update(Arg.Is<AppConfig>(c => c!.TicketActivationDelaySeconds == expected));
        vm.TicketActivationDelaySeconds.ShouldBe(expected);
    }

    [Fact]
    public void VersionText_Should_ShowInstalledVersion_When_Installed()
    {
        // Arrange / Act
        (SettingsViewModel vm, _) = NewVm(new AppConfig(), isInstalled: true, currentVersion: "1.1.7");

        // Assert
        vm.VersionText.ShouldBe("Version 1.1.7");
    }

    [Fact]
    public void VersionText_Should_ShowDevelopmentBuild_When_NotInstalled()
    {
        // Arrange / Act
        (SettingsViewModel vm, _) = NewVm(new AppConfig(), isInstalled: false);

        // Assert
        vm.VersionText.ShouldBe("Development build");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"relative\folder")]
    public void Save_Should_RejectAndStayOpen_When_DownloadDirectoryIsInvalid(string directory)
    {
        // Arrange
        (SettingsViewModel vm, IConfigService config) = NewVm(new AppConfig());
        vm.DownloadDirectory = directory;
        bool closed = false;
        vm.CloseRequested += () => closed = true;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        config.DidNotReceive().Update(Arg.Any<AppConfig>());
        vm.DownloadDirectoryError.ShouldNotBeNullOrEmpty();
        closed.ShouldBeFalse();
    }
}
