using System.Diagnostics;
using Octokit;

namespace BananaGit.Services;

/// <summary>
/// Uses <see href="https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#device-flow">GitHub Device Flow</see>
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

    /// <summary>
    /// Opens a GitHub log in page in the user's default browser
    /// </summary>
    /// <param name="userCode">The user code returned by GitHub</param>
    /// <returns>The GitHub access token</returns>
    /// <remarks>
    /// The GitHub access token is used by Lib2GitSharp as credentials to modify repositories
    /// </remarks>
    public async Task<string?> LoginAsync(Action<string, string> userCode)
    {
        try
        {
            var codeRequest = new OauthDeviceFlowRequest(ClientId)
            {
                Scopes = { "repo", "user" }
            };

            var codeResponse = await _githubClient.Oauth.InitiateDeviceFlow(codeRequest);

            //Show user device code
            userCode?.Invoke(codeResponse.UserCode, codeResponse.VerificationUri);

            //Create process to open browser for validation
            var validationProcess = new ProcessStartInfo(codeResponse.VerificationUri)
            {
                UseShellExecute = true
            };

            Process.Start(validationProcess);

            //Response from redirect
            var accessToken = await _githubClient.Oauth.CreateAccessTokenForDeviceFlow(ClientId, codeResponse);

            return accessToken.AccessToken;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to Authenticate: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all emails associated with the current logged in account and returns a verified or primary email
    /// </summary>
    /// <returns>Either the Users verified email or null</returns>
    public async Task<string?> GetUserEmail(string accessToken)
    {
        IReadOnlyList<EmailAddress>? emails = null;

        try
        {
            _githubClient.Credentials = new Credentials(accessToken);

            emails = await _githubClient.User.Email.GetAll();
        }
        catch (AuthorizationException)
        {
            Trace.WriteLine("Could not authenticate account.");
            return "Could not authenticate account";
        }

        //Try to get a verified email first 
        var verifiedEmail = emails.FirstOrDefault(x => x.Verified)?.Email;

        if (verifiedEmail != null)
            return verifiedEmail;

        //If there are no verified emails, try to get a primary email
        var primaryEmail = emails.FirstOrDefault(x => x.Primary)?.Email;

        return primaryEmail;
    }

    /// <summary>
    /// Gets the users GitHub display name
    /// </summary>
    /// <param name="accessToken">Authentication access token</param>
    /// <returns>GitHub username</returns>
    public async Task<string?> GetUsername(string accessToken)
    {
        User? userData;
        try
        {
            _githubClient.Credentials = new Credentials(accessToken);

            userData = await _githubClient.User.Current();
        }
        catch (AuthorizationException)
        {
            Trace.WriteLine("Could not authenticate account.");
            return "Could not authenticate account";
        }

        if (userData == null)
        {
            Trace.WriteLine("Could not access user data");
            return "Could not access user data";
        }

        return userData.Login;
    }
}