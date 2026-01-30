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
    /// <returns>The name of the default branch head</returns>
    public static string? GetDefaultRepoName(string repoUrl)
    {
        try
        {
            var gitProcessInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"ls-remote --symref \"{repoUrl}\" HEAD",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            using var process = Process.Start(gitProcessInfo);
            if (process == null) throw new NullReferenceException("Git info process couldn't start!");
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0) throw new NullReferenceException("Process didn't return anything!");

            if (output.StartsWith($"refs/remotes/origin/"))
            {
                return output.Substring($"refs/remotes/origin/".Length);
            }
            
            var match = System.Text.RegularExpressions.Regex.Match(output,  @"ref:\s*refs/heads/(\S+)\s+HEAD");
            
            return match.Success ? match.Groups[1].Value : null;
        }
        catch (LibGit2SharpException e)
        {
            Trace.WriteLine(e.Message);
        }
        return null;
    }
}