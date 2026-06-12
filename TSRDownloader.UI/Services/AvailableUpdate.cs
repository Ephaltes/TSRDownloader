using Velopack;

namespace TSRDownloader.UI.Services;

/// <summary>
/// A newer release found by <see cref="IUpdateService.CheckAsync"/>.
/// </summary>
/// <param name="Version">The new version, e.g. "1.1.7".</param>
public sealed record AvailableUpdate(string Version)
{
    /// <summary>The underlying Velopack update; null for updates created in tests.</summary>
    public UpdateInfo? VelopackInfo { get; init; }
}
