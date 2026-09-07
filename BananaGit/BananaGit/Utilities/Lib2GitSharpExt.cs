using System.Diagnostics;
using LibGit2Sharp;

namespace BananaGit.Utilities;

/// <summary>
/// Helper methods for Lib2GitSharp
/// </summary>
public static class Lib2GitSharpExt
{
    /// <summary>
    /// Finds the default head branch of a repository
    /// </summary>
    /// <param name="repoUrl">The repositories URL</param>
    /// <param name="token">The PAT</param>
    /// <returns>The name of the default branch head</returns>
    public static string? GetDefaultRepoName(string? repoUrl, string? token)
    {
        try
        {
            if (string.IsNullOrEmpty(repoUrl))
            {
                Trace.WriteLine("Couldn't find saved repository URL");
                return null;
            }

            string effectiveUrl = repoUrl;
            if (!string.IsNullOrEmpty(token) && repoUrl.StartsWith("https://"))
            {
                effectiveUrl = repoUrl.Replace("https://", $"https://{token}@");
            }

            var gitProcessInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"ls-remote --symref \"{effectiveUrl}\" HEAD",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(gitProcessInfo);
            if (process == null) throw new NullReferenceException("Git info process couldn't start!");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"git ls-remote failed (exit code {process.ExitCode}): {error}");

            if (output.StartsWith($"refs/remotes/origin/"))
            {
                return output.Substring($"refs/remotes/origin/".Length);
            }

            var match = System.Text.RegularExpressions.Regex.Match(output, @"ref:\s*refs/heads/(\S+)\s+HEAD");

            return match.Success ? match.Groups[1].Value : null;
        }
        catch (LibGit2SharpException e)
        {
            Trace.WriteLine(e.Message);
        }

        return null;
    }
}