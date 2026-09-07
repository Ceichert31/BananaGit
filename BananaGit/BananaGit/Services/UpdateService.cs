using Microsoft.Extensions.Configuration;
using Velopack;
using Velopack.Sources;

namespace BananaGit.Services;

/// <summary>
/// Uses the Velopack API to check for new updates
/// </summary>
public class UpdateService
{
    /// <summary>
    /// URL to the GitHub repository
    /// </summary>
    private const string GithubRepoUrl = "https://github.com/Ceichert31/BananaGit";

    private const bool DownloadPrerelease = true;

    private UpdateManager? _manager;
    private UpdateInfo? _newVersion;

    /// <summary>
    /// Initializes the Velopack API and update source
    /// </summary>
    public void Initialize()
    {
        var config = new ConfigurationBuilder().Build();

        var githubApiToken = config["GIT_TEST_TOKEN"];

        /*
        if (string.IsNullOrEmpty(githubApiToken))
            throw new NullReferenceException("Failed to fetch github token!");*/

        _manager = new UpdateManager(
            new GithubSource(GithubRepoUrl, githubApiToken, DownloadPrerelease)
        );
    }

    public UpdateInfo? GetUpdateInfo()
    {
        return _newVersion;
    }

    /// <summary>
    /// Checks if there are any new versions
    /// </summary>
    public async Task<bool> CheckForUpdateAsync()
    {
        if (_manager == null)
            throw new NullReferenceException("Failed to connect to update server!");

        _newVersion = await _manager.CheckForUpdatesAsync();

        return _newVersion != null;
    }

    /// <summary>
    /// Downloads the latest update locally,
    /// and then returns a signal that Velopack is ready to apply the update
    /// </summary>
    /// <returns>Signal that Velopack is ready to apply the update</returns>
    /// <exception cref="NullReferenceException">Thrown if the <see cref="UpdateManager"/> is null</exception>
    public async Task<bool> DownloadLatest()
    {
        if (_manager == null)
            throw new NullReferenceException("Failed to connect to update server!");

        // No new version
        if (_newVersion == null)
            return false;

        await _manager.DownloadUpdatesAsync(_newVersion);

        // Waits for the program to exit cleanly, then applies updates
        _manager.WaitExitThenApplyUpdates(_newVersion);

        // Signals that Velopack is ready to apply the update
        return true;
    }
}