using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Win32;

namespace BananaGit.ViewModels
{
    partial class GitInfoViewModel : ObservableObject
    {
        #region Properties
        [ObservableProperty]
        private string _repoName = string.Empty;
        [ObservableProperty]
        private string _commitMessage = string.Empty;


        //Clone properties
        [ObservableProperty]
        private string _localRepoFilePath = string.Empty;
        [ObservableProperty]
        private string _repoURL = string.Empty;

        //Conventional Commit drop-down
        [ObservableProperty]
        private string _selectedCommitHeader = string.Empty;

        //Branches
        [ObservableProperty]
        private string _branchName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _currentChanges = [];
        [ObservableProperty]
        private ObservableCollection<string> _stagedChanges = [];

        [ObservableProperty]
        private ObservableCollection<GitCommitInfo> _commitHistory = [];

        //Flags
        [ObservableProperty]
        private bool _canClone = false;

        [ObservableProperty]
        private bool _directoryHasFiles = false;

        [ObservableProperty]
        private bool _noRepoCloned;
        #endregion

        private readonly DispatcherTimer _updateGitInfoTimer = new();

        private GithubUserInfo? githubUserInfo;

        private EventHandler openCloneWindow;

        public GitInfoViewModel(EventHandler openCloneWindow) 
        {
            _updateGitInfoTimer.Tick += UpdateRepoStatus;
            _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _updateGitInfoTimer.Start();

            JsonDataManager.LoadUserInfo(ref githubUserInfo);

            this.openCloneWindow = openCloneWindow;
            PropertyChanged += UpdateCurrentRepository;

            Initialize();
        }

        /// <summary>
        /// Runs checks on whether local repo is valid
        /// </summary>
        /// <exception cref="RepoLocationException"></exception>
        private void Initialize()
        {
            try
            {
                //Save data missing
                if (githubUserInfo == null)
                {
                    throw new LoadDataException("ERROR: Data couldn't be loaded!");
                }

                //If no repos are cloned, display clone repo text
                if (githubUserInfo.SavedRepository == null)
                {
                    NoRepoCloned = true;
                    throw new InvalidRepoException("No saved repository after loading!");
                }
                else
                {
                    //Check if repo data is empty
                    if (githubUserInfo.SavedRepository.URL.Equals(string.Empty) &&
                       githubUserInfo.SavedRepository.FilePath.Equals(string.Empty))
                    {
                        throw new InvalidRepoException("Saved repository is empty!");
                    }

                    //Load current repo data if there is an already opened repo
                    LocalRepoFilePath = githubUserInfo.SavedRepository.FilePath;
                    RepoURL = githubUserInfo.SavedRepository.URL;
                }

                //Check if repo location exists
                if (!Directory.Exists(LocalRepoFilePath))
                {
                    NoRepoCloned = true;
                    throw new RepoLocationException("Local repository file location missing!");
                }

                //Check if directory is empty
                if (!Directory.EnumerateFiles(LocalRepoFilePath).Any())
                {
                    throw new RepoLocationException("Repository location is empty!");
                }

                NoRepoCloned = false;
            }
            catch (GitException ex)
            {
                //Output to debug console
                Console.WriteLine(ex.Message);
                NoRepoCloned = true;
            }
           
        }

        /// <summary>
        /// Updates the repository info and saves it when changed
        /// </summary>
        /// <param name="sender">The event caller</param>
        /// <param name="e">The event arguments</param>
        private void UpdateCurrentRepository(object? sender, PropertyChangedEventArgs e)
        {
            if (NoRepoCloned) return;

            if (githubUserInfo == null) return;
            if (githubUserInfo.SavedRepository == null) return;

            if (e.PropertyName == nameof(RepoURL))
            {
                githubUserInfo.SavedRepository.URL = RepoURL;
                JsonDataManager.SaveUserInfo(githubUserInfo);
            }
            else if (e.PropertyName == nameof(LocalRepoFilePath))
            {
                githubUserInfo.SavedRepository.FilePath = LocalRepoFilePath;
                JsonDataManager.SaveUserInfo(githubUserInfo);
            }
        }

        /// <summary>
        /// Update the current unstaged changes
        /// </summary>
        [RelayCommand]
        public void UpdateRepoStatus(object? sender, EventArgs e)
        {
            if (NoRepoCloned) return;
            try
            {
                if (LocalRepoFilePath == null || LocalRepoFilePath == "") return;

                if (!Directory.Exists(LocalRepoFilePath)) return;

                CurrentChanges.Clear();
                StagedChanges.Clear();

                using var repo = new Repository(LocalRepoFilePath);
                var stats = repo.RetrieveStatus(new StatusOptions());

                CommitHistory.Clear();
                //Update list of all commits
                var commits = repo.Commits;
                foreach (var item in commits)
                {
                    GitCommitInfo commitInfo = new();
                    commitInfo.Author = item.Author.ToString();
                    commitInfo.Date =
                        $"{item.Author.When.DateTime.ToLongTimeString()} {item.Author.When.DateTime.ToShortDateString()}";
                    commitInfo.Message = item.Message;
                    commitInfo.Commit = item.Id.ToString();
                    CommitHistory.Add(commitInfo);
                }

                //If no changes have been made, don't do anything
                if (!stats.IsDirty) return;

                var untracked = stats.Untracked;
                var changed = stats.Modified;
                var staged = stats.Staged;
                var added = stats.Added;
                var removed = stats.Removed;
                var missing = stats.Missing;
                foreach (var file in untracked)
                {
                    CurrentChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
                foreach (var file in removed)
                {
                    CurrentChanges.Add($"-{file.FilePath}" ?? "N/A");
                }
                foreach (var file in missing)
                {
                    CurrentChanges.Add($"-{file.FilePath}" ?? "N/A");
                }
                foreach (var file in changed)
                {
                    CurrentChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
                foreach (var file in staged)
                {
                    StagedChanges.Add($"+{file.FilePath}");
                }
                foreach (var file in added)
                {
                    StagedChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

       
        /// <summary>
        /// Commits all staged files and makes them ready to push
        /// </summary>
        [RelayCommand]
        public void CommitStagedFiles()
        {
            try
            {
                using (var repo = new Repository(LocalRepoFilePath))
                {
                    Signature author = new(githubUserInfo?.Username, "ceichert3114@gmail.com", DateTime.Now);
                    Signature committer = author;

                    //Author commit
                    Commit commit = repo.Commit($"{SelectedCommitHeader} {CommitMessage}", author, committer);

                    //Clear commit message
                    CommitMessage = string.Empty;
                }
            }
            catch (LibGit2SharpException)
            {
                Console.WriteLine($"Failed to commit {LocalRepoFilePath}");
                throw;
            }
        }

        [RelayCommand]
        public void StageFiles()
        {
            try
            {
                var files = Directory.EnumerateFiles(LocalRepoFilePath);

                using var repo = new Repository(LocalRepoFilePath);

                var removed = repo.RetrieveStatus().Removed;
                var missing = repo.RetrieveStatus().Missing;

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    repo.Index.Add(fileName);
                    repo.Index.Write();
                    //Commands.Stage(repo, file);
                }

                foreach (var file in missing)
                {
                /*    var fileName = Path.GetFileName();
                    repo.Index.Add(fileName);
                    repo.Index.Write();*/
                    Commands.Stage(repo, file.FilePath);
                }
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine($"Failed to stage {ex.Message}");
                throw;
            }
        }

        [RelayCommand]
        public void PushFiles()
        {
            try
            {
                using var repo = new Repository(LocalRepoFilePath);
                var remote = repo.Network.Remotes["origin"];
                if (remote != null)
                {
                    repo.Network.Remotes.Remove("origin");
                }

                repo.Network.Remotes.Add("origin", RepoURL);
                remote = repo.Network.Remotes["origin"];
                if (remote == null) return;

                FetchOptions options = new FetchOptions
                {
                    CredentialsProvider = (url, username, types) =>
                    new UsernamePasswordCredentials
                    {
                        Username = githubUserInfo?.Username,
                        Password = githubUserInfo?.PersonalToken
                    }
                };

                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(repo, remote.Name, refSpecs, options, string.Empty);

                var localBranchName = string.IsNullOrEmpty(BranchName) ? "main" : BranchName;
                var localBranch = repo.Branches[localBranchName];

                if (localBranch == null) return;

                repo.Branches.Update(localBranch,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = localBranch.CanonicalName);

                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (url, username, types) =>
                    new UsernamePasswordCredentials
                    {
                        Username = githubUserInfo?.Username,
                        Password = githubUserInfo?.PersonalToken
                    }
                };

                repo.Network.Push(localBranch, pushOptions);
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine($"Failed to Push {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pulls changes from the repo and merges them into the local repository
        /// </summary>
        [RelayCommand]
        public void PullChanges()
        {
            try
            {
                var options = new PullOptions();
                options.FetchOptions = new FetchOptions();
                options.FetchOptions.CredentialsProvider = new CredentialsHandler((url, username, types) => new UsernamePasswordCredentials
                {
                    Username = githubUserInfo?.Username, 
                    Password = githubUserInfo?.PersonalToken
                });

                options.MergeOptions = new MergeOptions();
                options.MergeOptions.FastForwardStrategy = FastForwardStrategy.Default;
                options.MergeOptions.OnCheckoutNotify = new CheckoutNotifyHandler(ShowConflict);
                options.MergeOptions.CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict;
                using (var repo = new Repository(LocalRepoFilePath))
                {

                    //Create signature and pull
                    Signature signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                    var result = Commands.Pull(repo, signature, options);

                    //Check for merge conflicts
                    if (result.Status == MergeStatus.Conflicts)
                    {
                        //Display in front end eventually
                        Console.WriteLine("Conflict detected");
                        return;
                    }
                    else if (result.Status == MergeStatus.UpToDate)
                    {
                        //Display in front end eventually
                        Console.WriteLine("Up to date");
                        return;
                    }

                    Console.WriteLine("Pulled Successfuly");
                }
            }
            catch (LibGit2SharpException)
            {
                throw;
            }
        }

        /// <summary>
        /// The callback for conflicts
        /// </summary>
        /// <param name="path">The file that conflicted</param>
        /// <param name="notifyFlags">The checkout notify flag</param>
        /// <returns></returns>
        private bool ShowConflict(string path, CheckoutNotifyFlags notifyFlags)
        {
            if (notifyFlags == CheckoutNotifyFlags.Conflict)
            {
                Console.WriteLine($"Conflict found in file {path}");
            }
            return true;
        }

        [RelayCommand]
        public void OpenCloneWindow()
        {
            //Notify that we want to open window
            openCloneWindow.Invoke(this, new());
        }

        [RelayCommand]
        public void CloneRepo()
        {
            if (githubUserInfo == null) return;

            CloneRepo(githubUserInfo);
        }

        /// <summary>
        /// Opens a windows prompt to select the desired file directory
        /// </summary>
        [RelayCommand]
        public void ChooseCloneDirectory()
        {
            try
            {
                OpenFolderDialog dialog = new OpenFolderDialog();
                dialog.Multiselect = false;

                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                if (dialog.ShowDialog() == true)
                {
                    string selectedFilePath = dialog.FolderName;

                    //Check if directory is empty
                    if (!Directory.EnumerateFiles(selectedFilePath).Any())
                    {
                        CanClone = true;
                        LocalRepoFilePath = dialog.FolderName;
                        DirectoryHasFiles = false;
                    }
                    else
                    {
                        //Check if file location is local repo
                        var repo = new Repository(selectedFilePath);
                        CanClone = true;

                        //Set active repo as locally opened repo
                        LocalRepoFilePath = dialog.FolderName;
                        RepoURL = repo.Network.Remotes["origin"].Url;
                        githubUserInfo.SavedRepository.FilePath = LocalRepoFilePath;
                        githubUserInfo.SavedRepository.URL = RepoURL;
                        JsonDataManager.SaveUserInfo(githubUserInfo);
                        hasCloned = true;
                    }
                }
            }
            catch (LibGit2SharpException)
            {
                CanClone = false;
                DirectoryHasFiles = true;
            }
        }

        /// <summary>
        /// Clone a repository
        /// </summary>
        /// <param name="repoURL">The repositories URL</param>
        /// <param name="repoPath">Where the repository should be cloned to</param>
        /// <param name="username">The username of the user</param>
        /// <param name="token">The personal access token</param>
        /// <returns></returns>
        private void CloneRepo(GithubUserInfo userInfo)
        {
            try
            {
                var options = new CloneOptions
                {
                    FetchOptions =
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                            {
                                Username = userInfo.Username,
                                Password = userInfo.PersonalToken
                            }
                    }
                };

                //Save repo to github user info
                userInfo.SavedRepository.FilePath = LocalRepoFilePath;
                userInfo.SavedRepository.URL = RepoURL;
                JsonDataManager.SaveUserInfo(userInfo);
                Repository.Clone(RepoURL, LocalRepoFilePath, options);
                hasCloned = true;
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine($"Failed to Clone {ex.Message}");
                throw;
            }
            finally
            {
                JsonDataManager.SaveUserInfo(githubUserInfo);
            }
        }
    }

    public class GitCommitInfo
    {
        public string? Author { get; set; }
        public string? Date { get; set; }
        public string? Message { get; set; }
        public string? Commit { get; set; }
    }
}
