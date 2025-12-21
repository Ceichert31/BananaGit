using System.Windows.Threading;

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
        public LoadedRepositoryInfo? SavedRepository { get; set; }

        public void CopyContents(GitInfoModel gitInfo)
        {
            Username = gitInfo.Username;
            Email = gitInfo.Email;
            PersonalToken = gitInfo.PersonalToken;
            SavedRepository = gitInfo.SavedRepository;
        }
        public string? GetPath()
        {
            return SavedRepository?.FilePath;
        }
        public string? GetUrl()
        {
            return SavedRepository?.Url;
        }
        public void SetPath(string path) => SavedRepository.FilePath = path;
        public void SetUrl(string url) => SavedRepository.Url = url;

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
    public class LoadedRepositoryInfo(string path, string url)
    {
        /// <summary>
        /// Whether a repository is cloned or not
        /// </summary>
        public bool IsRepositoryCloned
        {
            get => _isRepositoryCloned;
            set
            {
                _isRepositoryCloned = value;
                OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private bool _isRepositoryCloned = false;

        /// <summary>
        /// The current branch that changes will be made to
        /// </summary>
        public GitBranch CurrentBranch
        {
            get => _currentBranch;
            set
            {
                _currentBranch = value;
                OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private GitBranch _currentBranch = new GitBranch();

        /// <summary>
        /// The file path to the local repository
        /// </summary>
        public string FilePath
        {
            get => _path;
            set
            {
                _path = value;
                OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private string _path = path;

        /// <summary>
        /// The URL to the remote repository
        /// </summary>
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private string _url = url;

        /// <summary>
        /// Invoked when LoadedRepository properties are changed
        /// </summary>
        public EventHandler? OnRepositoryChanged { get; set; }
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
