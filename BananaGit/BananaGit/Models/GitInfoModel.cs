namespace BananaGit.Models
{
    /// <summary>
    /// Holds all the users GitHub and repository information
    /// </summary>
    public class GitInfoModel
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PersonalToken { get; set; } 
        public SavableRepository? SavedRepository { get; set; }
        
        public void SetPath(string path) => SavedRepository?.FilePath = path;
        public void SetUrl(string url) => SavedRepository?.Url = url;
    }
    /// <summary>
    /// Holds repository information
    /// </summary>
    /// <param name="path">The path to the saved repository</param>
    /// <param name="url">The url for the saved repository</param>
    public class SavableRepository(string path, string url)
    {
        public string FilePath { get; set; } = path;
        public string Url { get; set; } = url;
    }
    /// <summary>
    /// Move this to its own model
    /// </summary>
    public class GitCommitInfo
    {
        public string? Author { get; set; }
        public string? Date { get; set; }
        public string? Message { get; set; }
        public string? Commit { get; set; }
    }
}
