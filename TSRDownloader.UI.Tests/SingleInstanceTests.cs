using System;
using System.Threading;
using Shouldly;
using TSRDownloader.UI.Services;
using Xunit;

namespace TSRDownloader.UI.Tests;

public class SingleInstanceTests
{
    private static string UniqueName() => "TSRDownloader.Tests." + Guid.NewGuid().ToString("N");

    [Fact]
    public void IsFirstInstance_Should_BeFalse_When_AnotherInstanceHoldsTheName()
    {
        // Arrange
        string name = UniqueName();
        using SingleInstance first = new(name);

        // Act
        using SingleInstance second = new(name);

        // Assert
        first.IsFirstInstance.ShouldBeTrue();
        second.IsFirstInstance.ShouldBeFalse();
    }

    [Fact]
    public void SignalFirstInstance_Should_InvokeTheFirstInstancesCallback_When_SecondInstanceStarts()
    {
        // Arrange
        string name = UniqueName();
        using SingleInstance first = new(name);
        using ManualResetEventSlim shown = new();
        first.ListenForSecondInstances(shown.Set);

        // Act
        using SingleInstance second = new(name);
        second.SignalFirstInstance();

        // Assert
        shown.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }
}
