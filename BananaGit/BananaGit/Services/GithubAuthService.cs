using System.Diagnostics;
using System;
using Octokit;

namespace BananaGit.Services;

/// <summary>
/// Verifies user login
/// </summary>
public class GithubAuthService
{
    private const string ClientId = "Ov23liz6gkcsl8SP4P38";
    private readonly GitHubClient _githubClient;

    public GithubAuthService()
    {
        _githubClient = new GitHubClient(new ProductHeaderValue("BananaGit"));
    }

    public async Task<string?> LoginAsync(Action<string, string> userCode)
    {
        try
        {
            var codeRequest = new OauthDeviceFlowRequest(ClientId)
            {
                Scopes = { "repo", "user" }
            };

            var codeResponse = await _githubClient.Oauth.InitiateDeviceFlow(codeRequest);

            //Create process to open browser for validation
            var validationProcess = new ProcessStartInfo(codeResponse.VerificationUri)
            {
                UseShellExecute = true,
                CreateNoWindow = true
            };

            //var tokenResponse = await _githubClient.Oauth.
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to Authenticate: {ex.Message}");
            return null;
        }

        return null;
    }
}