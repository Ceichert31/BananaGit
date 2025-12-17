using System.Diagnostics;
using LibGit2Sharp;

namespace BananaGit.Utilities;

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
            using var repo = new Repository(repoUrl);

            //Get list of remote branches
            var remotes = Repository.ListRemoteReferences(repoUrl);
            
            //Cache the first instance of /HEAD
            var headReference = remotes.FirstOrDefault(x => x.CanonicalName.EndsWith("/HEAD"));

            if (headReference != null)
            {
                //Splits the target identifier by / and takes the actual name
                return headReference.TargetIdentifier.Split('/').Last();
            }
        }
        catch (LibGit2SharpException e)
        {
            Trace.WriteLine(e.Message);
        }
        return null;
    }
}