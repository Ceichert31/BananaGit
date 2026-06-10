namespace BananaGit.EventArgExtensions;

/// <summary>
/// Contains device code for logging into GitHub via OAuth
/// </summary>
/// <param name="code"></param>
public class DeviceCodeEventArgs(string code) : System.EventArgs
{
    public string DeviceCode { get; private set; } = code;
}