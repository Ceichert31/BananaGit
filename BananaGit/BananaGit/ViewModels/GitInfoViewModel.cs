using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
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

        [ObservableProperty]
        private bool _hasCommitedFiles;
        #endregion

        private readonly DispatcherTimer _updateGitInfoTimer = new();

        private GitInfoModel? githubUserInfo;

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
                    if (!githubUserInfo.SavedRepository.IsValidRepository())
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

              /*  //Check if directory is empty
                if (!Directory.EnumerateFiles(LocalRepoFilePath))
                {
                    throw new RepoLocationException("Repository location is empty!");
                }*/

                NoRepoCloned = false;
            }
            catch (GitException ex)
            {
                //Output to debug console
                OutputError(ex.Message);
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
        /// Checks current repositories file location before using it
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="RepoLocationException"></exception>
        private void VerifyPath(string path)
        {
            try
            {
                if (LocalRepoFilePath == null || LocalRepoFilePath == "")
                {
                    throw new RepoLocationException("Local repository file path is empty!");
                }

                if (!Directory.Exists(LocalRepoFilePath))
                {
                    throw new RepoLocationException("Local repository file path is missing!");
                }
            }
            catch (GitException ex)
            {
                NoRepoCloned = true;
                OutputError(ex.Message);
            }
           
        }
        /// <summary>
        /// Throws errors on the main thread
        /// </summary>
        /// <param name="message"></param>
        private static void OutputError(string message)
        {
            Application.Current.Dispatcher.Invoke(() => { Trace.WriteLine(message); });
        }

        /// <summary>
        /// Update the current unstaged changes
        /// </summary>
        public void UpdateRepoStatus(object? sender, EventArgs e)
        {
            if (NoRepoCloned) return;
            try
            {
                VerifyPath(LocalRepoFilePath);

                CurrentChanges.Clear();
                StagedChanges.Clear();

                using var repo = new Repository(LocalRepoFilePath);
                var stats = repo.RetrieveStatus(new StatusOptions());

                CommitHistory.Clear();
                //Update list of all commits
                var commits = repo.Commits.ToList();
                foreach (var item in commits)
                {
                    GitCommitInfo commitInfo = new()
                    {
                        Author = item.Author.ToString(),
                        Date =
                        $"{item.Author.When.DateTime.ToLongTimeString()} {item.Author.When.DateTime.ToShortDateString()}",
                        Message = item.Message,
                        Commit = item.Id.ToString()
                    };
                    CommitHistory.Add(commitInfo);
                }

                //If no changes have been made, don't do anything
                if (!stats.IsDirty) return;
                
                foreach (var file in stats)
                {
                    //Changes logic
                    if (file.State == FileStatus.ModifiedInWorkdir || file.State == FileStatus.NewInWorkdir || file.State == FileStatus.RenamedInWorkdir)
                    {
                        CurrentChanges.Add($"+{file.FilePath.GetName()}");
                    }
                    else if (file.State == FileStatus.DeletedFromWorkdir)
                    {
                        CurrentChanges.Add($"-{file.FilePath.GetName()}");
                    }
                    //Staging logic
                    else if (file.State == FileStatus.ModifiedInIndex || file.State == FileStatus.NewInIndex || file.State == FileStatus.RenamedInIndex)
                    {
                        StagedChanges.Add($"+{file.FilePath.GetName()}");
                    }
                    else if (file.State == FileStatus.DeletedFromIndex)
                    {
                        StagedChanges.Add($"-{file.FilePath.GetName()}");
                    }
                }
            }
            catch (GitException ex)
            {
                OutputError(ex.Message);
            }
            catch (LibGit2SharpException ex)
            {
                OutputError(ex.Message);
            }
        }

        #region Stage/Commit
        /// <summary>
        /// Commits all staged files and makes them ready to push
        /// </summary>
        [RelayCommand]
        public void CommitStagedFiles()
        {
            try
            {
                VerifyPath(LocalRepoFilePath);

                using (var repo = new Repository(LocalRepoFilePath))
                {
                    Signature author = new(githubUserInfo?.Username, githubUserInfo?.Email, DateTime.Now);
                    Signature committer = author;

                    //Author commit
                    Commit commit = repo.Commit($"{SelectedCommitHeader} {CommitMessage}", author, committer);

                    //Clear commit message
                    CommitMessage = string.Empty;

                    HasCommitedFiles = true;
                }
            }
            catch (LibGit2SharpException)
            {
                OutputError($"Failed to commit {LocalRepoFilePath}");
            }
        }

        [RelayCommand]
        public void StageFiles()
        {
            try
            {
                VerifyPath(LocalRepoFilePath);

                using var repo = new Repository(LocalRepoFilePath);

                var status = repo.RetrieveStatus();
                if (!status.IsDirty) return;


                foreach (var file in status)
                {
                    if (file.State == FileStatus.Ignored) continue;

                    Commands.Stage(repo, file.FilePath);
                }
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to stage {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Push/Pull
        /// <summary>
        /// Pushes files to current branch (Only main for right now)
        /// </summary>
        [RelayCommand]
        public void PushFiles()
        {
            //Pull before pushing
            PullChanges();

            Task.Run(() =>
            {
                try
                {
                    VerifyPath(LocalRepoFilePath);

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
                    HasCommitedFiles = false;
                }
                catch (LibGit2SharpException ex)
                {
                    OutputError($"Failed to Push {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Pulls changes from the repo and merges them into the local repository
        /// </summary>
        [RelayCommand]
        public void PullChanges()
        {
            Task.Run(() => {
                try
                {
                    VerifyPath(LocalRepoFilePath);

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
                            OutputError("Conflict detected");
                            return;
                        }
                        else if (result.Status == MergeStatus.UpToDate)
                        {
                            //Display in front end eventually
                            OutputError("Up to date");
                            return;
                        }

                        OutputError("Pulled Successfuly");
                    }
                }
                catch (LibGit2SharpException ex)
                {
                    OutputError(ex.Message);
                }
            });
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
                Trace.WriteLine($"Conflict found in file {path}");
            }
            return true;
        }

        #endregion

        #region Clone 
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

            VerifyPath(LocalRepoFilePath);

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
                if (githubUserInfo == null)
                {
                    throw new LoadDataException("No Loaded data!");
                }

                //Open file select dialogue
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

                        //Set active repo as locally opened repo
                        LocalRepoFilePath = dialog.FolderName;
                        RepoURL = repo.Network.Remotes["origin"].Url;

                        //Save to user info
                        githubUserInfo.SavedRepository = new(LocalRepoFilePath, RepoURL);
                        JsonDataManager.SaveUserInfo(githubUserInfo);

                        //Set flags
                        CanClone = true;
                        DirectoryHasFiles = false;
                        NoRepoCloned = false;
                    }
                }
            }
            catch (LibGit2SharpException ex)
            {
                CanClone = false;
                NoRepoCloned = true;
                DirectoryHasFiles = true;
                Trace.WriteLine(ex.Message);
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
        private void CloneRepo(GitInfoModel userInfo)
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
                userInfo.SavedRepository = new(LocalRepoFilePath, RepoURL);
                JsonDataManager.SaveUserInfo(userInfo);
                Repository.Clone(RepoURL, LocalRepoFilePath, options);
                NoRepoCloned = false;
            }
            catch (LibGit2SharpException ex)
            {
                Trace.WriteLine($"Failed to Clone {ex.Message}");
            }
        }
        #endregion
    }
}
