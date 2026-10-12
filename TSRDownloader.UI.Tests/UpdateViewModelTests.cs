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
        UpdateViewModel viewModel = new("Updating to version 1.1.7…", isBlocked: false);
        bool closeRequested = false;
        viewModel.CloseRequested += () => closeRequested = true;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        viewModel.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        closeRequested.ShouldBeTrue();
    }

    [Fact]
    public void Close_Should_RequestCloseWithoutCancelling_When_Blocked()
    {
        // Arrange
        UpdateViewModel viewModel = new("Downloads are in progress.", isBlocked: true);
        bool closeRequested = false;
        viewModel.CloseRequested += () => closeRequested = true;

        // Act
        viewModel.CloseCommand.Execute(null);

        // Assert
        closeRequested.ShouldBeTrue();
        viewModel.IsBlocked.ShouldBeTrue();
    }

    [Fact]
    public void Progress_Should_RaisePropertyChanged_When_Set()
    {
        // Arrange
        UpdateViewModel viewModel = new("Updating…", isBlocked: false);
        string? changedProperty = null;
        viewModel.PropertyChanged += (_, eventArgs) => changedProperty = eventArgs.PropertyName;

        // Act
        viewModel.Progress = 42;

        // Assert
        viewModel.Progress.ShouldBe(42);
        changedProperty.ShouldBe(nameof(UpdateViewModel.Progress));
    }
}
