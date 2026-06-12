using Shouldly;
using TSRDownloader.UI.ViewModels;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class UpdateViewModelTests
{
    [Fact]
    public void Cancel_Should_CancelTokenAndRequestClose_When_Executed()
    {
        // Arrange
        UpdateViewModel vm = new("Updating to version 1.1.7…", isBlocked: false);
        bool closeRequested = false;
        vm.CloseRequested += () => closeRequested = true;

        // Act
        vm.CancelCommand.Execute(null);

        // Assert
        vm.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        closeRequested.ShouldBeTrue();
    }

    [Fact]
    public void Close_Should_RequestCloseWithoutCancelling_When_Blocked()
    {
        // Arrange
        UpdateViewModel vm = new("Downloads are in progress.", isBlocked: true);
        bool closeRequested = false;
        vm.CloseRequested += () => closeRequested = true;

        // Act
        vm.CloseCommand.Execute(null);

        // Assert
        closeRequested.ShouldBeTrue();
        vm.IsBlocked.ShouldBeTrue();
    }

    [Fact]
    public void Progress_Should_RaisePropertyChanged_When_Set()
    {
        // Arrange
        UpdateViewModel vm = new("Updating…", isBlocked: false);
        string? changedProperty = null;
        vm.PropertyChanged += (_, args) => changedProperty = args.PropertyName;

        // Act
        vm.Progress = 42;

        // Assert
        vm.Progress.ShouldBe(42);
        changedProperty.ShouldBe(nameof(UpdateViewModel.Progress));
    }
}
