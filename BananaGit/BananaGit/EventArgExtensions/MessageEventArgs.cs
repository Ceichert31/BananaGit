namespace BananaGit.EventArgExtensions;

/// <summary>
/// Used for exception handling and sending messages to console with gitservice
/// </summary>
public class MessageEventArgs(string message) : System.EventArgs
{
    public string Message { get; set; } = message;
}