using Velopack;
using Velopack.Sources;

namespace TSRDownloader.UI.Services;

/// <summary>
/// <see cref="IUpdateService"/> backed by Velopack, reading the public GitHub Releases of the repository.
/// This is the only class that talks to the Velopack update API.
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    private const string RepositoryUrl = "https://github.com/Ephaltes/TSRDownloader";

    private readonly UpdateManager _updateManager =
        new(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));

    public bool IsInstalled => _updateManager.IsInstalled;

    public string? CurrentVersion => _updateManager.CurrentVersion?.ToString();

    public async Task<AvailableUpdate?> CheckAsync(CancellationToken cancellationToken)
    {
        UpdateInfo? updateInfo = await _updateManager.CheckForUpdatesAsync().WaitAsync(cancellationToken);
        if (updateInfo is null)
            return null;

        return new AvailableUpdate(updateInfo.TargetFullRelease.Version.ToString())
        {
            VelopackInfo = updateInfo
        };
    }

    public Task DownloadAsync(AvailableUpdate update, Action<int> progress, CancellationToken cancellationToken) =>
        _updateManager.DownloadUpdatesAsync(GetInfo(update), progress, cancellationToken);

    public void ApplyAndRestart(AvailableUpdate update) =>
        _updateManager.ApplyUpdatesAndRestart(GetInfo(update).TargetFullRelease);

    private static UpdateInfo GetInfo(AvailableUpdate update) =>
        update.VelopackInfo
        ?? throw new InvalidOperationException($"Update {update.Version} has no Velopack release information.");
}
