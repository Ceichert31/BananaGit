namespace BananaGit.EventArgExtensions;

/// <summary>
/// Event Arguments for login status
/// </summary>
public class CredentialsEventArgs(bool success) : System.EventArgs
{
    /// <summary>
    /// True if user successfully logged in, false otherwise
    /// </summary>
    public bool LoginSuccess { get; set; } = success;
}