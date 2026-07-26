namespace BananaGit.Models
{
    /// <summary>
    /// Holds all the users GitHub and repository information
    /// </summary>
    public class GitInfoModel
    {
        /// <summary>
        /// The users username
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// The users primary email address
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// The users personal access token for Git permissions
        /// </summary>
        public string? PersonalToken { get; set; }

        /// <summary>
        /// The locally saved repository that was last opened
        /// </summary>
        public SavableRepository? SavedRepository { get; set; }

        /// <summary>
        /// The current branch git operations are being performed on
        /// </summary>
        public GitBranch? CurrentBranch { get; set; }

        /// <summary>
        /// All branches that the user has checked out
        /// </summary>
        public List<string> VisibleBranches { get; set; } = [];

        /// <summary>
        /// Gets the cached local repositories path
        /// </summary>
        /// <returns>Either a string or null if no local repository is cached</returns>
        public string? GetPath()
        {
            return SavedRepository?.FilePath;
        }

        /// <summary>
        /// Tries to get the repository path
        /// </summary>
        /// <param name="output">The output for the path</param>
        /// <returns>Whether the path is valid or not</returns>
        public bool TryGetPath(out string output)
        {
            if (SavedRepository?.FilePath != null)
            {
                output = SavedRepository?.FilePath ?? string.Empty;
                return true;
            }

            output = string.Empty;
            return false;
        }

        /// <summary>
        /// Gets the cached local repositories URL
        /// </summary>
        /// <returns>Either a string or null if no local repository is cached</returns>
        public string? GetUrl()
        {
            return SavedRepository?.Url;
        }

        /// <summary>
        /// Sets the local repository path
        /// </summary>
        /// <param name="path">File path to the local repository</param>
        public void SetPath(string path)
        {
            SavedRepository ??= new("", "");

            SavedRepository.FilePath = path;
        }

        /// <summary>
        /// Sets the local repository URL
        /// </summary>
        /// <param name="url">URL to access the remote repository </param>
        public void SetUrl(string url)
        {
            SavedRepository ??= new("", "");

            SavedRepository.Url = url;
        }

        /// <summary>
        /// Verifies the saved repo is not null
        /// </summary>
        /// <returns>Whether repo is null</returns>
        public bool IsSavedRepositoryValid()
        {
            return SavedRepository != null &&
                   SavedRepository.FilePath != null &&
                   SavedRepository.Url != null;
        }
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
}