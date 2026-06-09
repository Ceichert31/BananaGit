using System.Diagnostics;
using Octokit;

namespace BananaGit.Services;

/// <summary>
/// Uses <see href="https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps">GitHub Device Flow</see>
/// to access user's repositories and credentials
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

            Process.Start(validationProcess);

            //Response from redirect
            var tokenResponse =
                await _githubClient.Authorization.CheckApplicationAuthentication(ClientId, codeResponse.DeviceCode);

            return tokenResponse.Token;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to Authenticate: {ex.Message}");
            return null;
        }
    }
}