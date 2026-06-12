using System;
using TSRDownloader.UI.Services;

namespace TSRDownloader.UI.Tests;

/// <summary>Test dispatcher that runs the action synchronously on the current thread.</summary>
public sealed class ImmediateDispatcher : IUiDispatcher
{
    public void Invoke(Action action) => action();
}
