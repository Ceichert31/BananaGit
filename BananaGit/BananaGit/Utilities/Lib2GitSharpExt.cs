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
    public static string? GetDefaultRepoName(string repoUrl, FetchOptions options)
    {
        try
        {
            //Get list of remote branches
            var remotes = Repository.ListRemoteReferences(repoUrl, options.CredentialsProvider);
            
            //Cache the first instance of /HEAD
            var headReference = remotes.FirstOrDefault(x => x.CanonicalName == "HEAD" && x.CanonicalName.StartsWith("refs/remotes"));

            if (headReference != null)
            {
                var targetRef = headReference.TargetIdentifier;
                
                //Return last element of target ref as fallback
                return targetRef.Split('/').Last();
            }
        }
        catch (LibGit2SharpException e)
        {
            Trace.WriteLine(e.Message);
        }
        return null;
    }
}