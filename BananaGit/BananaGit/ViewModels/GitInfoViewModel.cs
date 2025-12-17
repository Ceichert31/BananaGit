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

        [ObservableProperty]
        private ObservableCollection<ChangedFile> _currentChanges = [];
        [ObservableProperty]
        private ObservableCollection<ChangedFile> _stagedChanges = [];

        [ObservableProperty]
        private ObservableCollection<GitBranch> _localBranches = [];

        [ObservableProperty]
        private ObservableCollection<GitBranch> _remoteBranches = [];

        [ObservableProperty]
        private GitBranch? _currentBranch;

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

        [ObservableProperty]
        private bool _isTutorialOpen;
        #endregion

        private readonly DispatcherTimer _updateGitInfoTimer = new();

        private GitInfoModel? githubUserInfo;

        private int _commitHistoryLength = 30;

        private readonly DialogService _dialogService;

        public GitInfoViewModel()
        {
            _dialogService = new DialogService(this);
            
            _updateGitInfoTimer.Tick += UpdateRepoStatus;
            _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _updateGitInfoTimer.Start();

            JsonDataManager.LoadUserInfo(ref githubUserInfo);

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
                //Create new git branch
                CurrentBranch = new GitBranch();

                //Load current repo data if there is an already opened repo
                LocalRepoFilePath = githubUserInfo.SavedRepository.FilePath;
                RepoURL = githubUserInfo.SavedRepository.URL;

                //Update that we succesfully initialized the repository
                NoRepoCloned = false;

                UpdateBranches(new Repository(LocalRepoFilePath), CurrentBranch);
            }
            catch (GitException ex)
            {
                OutputError(ex.Message);
                NoRepoCloned = true;
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
                NoRepoCloned = true;
            }
        }

        #region Update Methods
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
                if (CurrentBranch == null || CurrentBranch.Name == "") throw new NullReferenceException("Current branch isn't set");

                var currentBranch = repo.Branches[CurrentBranch.Name] ?? throw new NullReferenceException("Current branch isn't set");

                var commits = currentBranch.Commits.ToList();

                //Limits commit history to a certain length
                for (int i = 0; i < _commitHistoryLength; ++i)
                {
                    var item = commits[i];
                    GitCommitInfo commitInfo = new()
                    {
                        Author = item.Author.ToString(),
                        Date =
                       $"{item.Author.When.DateTime.ToShortTimeString()} {item.Author.When.DateTime.ToShortDateString()}",
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
                    if (file.State == FileStatus.ModifiedInWorkdir || file.State == FileStatus.NewInWorkdir ||
                        file.State == FileStatus.RenamedInWorkdir || file.State == FileStatus.DeletedFromWorkdir ||
                        file.State == (FileStatus.NewInIndex | FileStatus.ModifiedInWorkdir))
                    {
                        CurrentChanges.Add(new(file, file.FilePath));
                    }
                    //Staging logic
                    else if (file.State == FileStatus.ModifiedInIndex || file.State == FileStatus.NewInIndex
                        || file.State == FileStatus.RenamedInIndex || file.State == FileStatus.DeletedFromIndex)
                    {
                        StagedChanges.Add(new(file, file.FilePath));
                    }
                }
            }
            catch (LibGit2SharpException ex)
            {
                OutputError(ex.Message);
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }

        /// <summary>
        /// Checks if any new branches were added and adds them to list
        /// </summary>
        /// <param name="repo"></param>
        private void UpdateBranches(Repository repo, GitBranch currentBranch)
        {
            try
            {
                LocalBranches.Add(currentBranch);

                //Set fetch options to prune any old remote branches
                var fetchOptions = new FetchOptions { Prune = true };

                //Cache first remote
                var remote = repo.Network.Remotes.FirstOrDefault();

                //Prune remote branches
                if (remote != null)
                {
                    Commands.Fetch(repo, remote.Name, new string[0], fetchOptions, "");
                }
                else
                {
                    throw new NullReferenceException("Couldn't prune remote branches!");
                }

                //Update branch data on a seperate thread
                Application.Current.Dispatcher.Invoke((() =>
                {
                    //Update branches
                    foreach (var branch in repo.Branches)
                    {
                        if (branch.FriendlyName == repo.Head.FriendlyName) continue;

                        if (branch.IsRemote)
                        {
                            //Filter out all remotes with ref in the name
                            if (branch.FriendlyName.Contains("refs"))
                                continue;

                            //Check if branch already exists
                            if (RemoteBranches.Where(x => x.Branch == branch).Any())
                                continue;

                            RemoteBranches.Add(new GitBranch(branch));
                            continue;
                        }

                        //Check if local branch already exists
                        if (LocalBranches.Where(x => x.Branch == branch).Any())
                            continue;

                        LocalBranches.Add(new GitBranch(branch));
                    }
                }));
                //Update current branch
                CurrentBranch = currentBranch;
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
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
                Trace.WriteLine($"Conflict found in file {path}");
            }
            return true;
        }

        #endregion

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

                using var repo = new Repository(LocalRepoFilePath);

                Signature author = new(githubUserInfo?.Username, githubUserInfo?.Email, DateTime.Now);
                Signature committer = author;

                //Author commit
                Commit commit = repo.Commit($"{SelectedCommitHeader} {CommitMessage}", author, committer);

                //Clear commit message
                CommitMessage = string.Empty;

                HasCommitedFiles = true;
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to commit {LocalRepoFilePath}");
                OutputError(ex.Message);
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }

        [RelayCommand]
        public void StageFiles()
        {
            Task.Run(() =>
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
                catch (Exception ex)
                {
                    OutputError(ex.Message);
                }
            });
        }

        [RelayCommand]
        public void StageFile(ChangedFile file)
        {
            try
            {
                VerifyPath(LocalRepoFilePath);

                using var repo = new Repository(LocalRepoFilePath);

                var status = repo.RetrieveStatus();
                if (!status.IsDirty) return;

                Commands.Stage(repo, file.FilePath);
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to stage {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }

        [RelayCommand]
        public void UnstageFile(ChangedFile file)
        {
            try
            {
                VerifyPath(LocalRepoFilePath);

                using var repo = new Repository(LocalRepoFilePath);

                var status = repo.RetrieveStatus();
                if (!status.IsDirty) return;

                Commands.Unstage(repo, file.FilePath);
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to stage {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
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
            Task.Run(() =>
            {
                try
                {
                    VerifyPath(LocalRepoFilePath);

                    using var repo = new Repository(LocalRepoFilePath);
                    var remote = repo.Network.Remotes[CurrentBranch.Name];
                    if (remote != null)
                    {
                        repo.Network.Remotes.Remove(CurrentBranch.Name);
                    }

                    repo.Network.Remotes.Add(CurrentBranch.Name, RepoURL);
                    remote = repo.Network.Remotes[CurrentBranch.Name];
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

                    var localBranchName = string.IsNullOrEmpty(CurrentBranch.Name) ? repo.Head.FriendlyName : CurrentBranch.Name;
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
                catch (Exception ex)
                {
                    OutputError(ex.Message);
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

                    var options = new PullOptions
                    {
                        FetchOptions = new FetchOptions()
                    };
                    options.FetchOptions.CredentialsProvider = new CredentialsHandler((url, username, types) => new UsernamePasswordCredentials
                    {
                        Username = githubUserInfo?.Username,
                        Password = githubUserInfo?.PersonalToken
                    });

                    options.MergeOptions = new MergeOptions
                    {
                        FastForwardStrategy = FastForwardStrategy.Default,
                        OnCheckoutNotify = new CheckoutNotifyHandler(ShowConflict),
                        CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict
                    };

                    using var repo = new Repository(LocalRepoFilePath);

                    UpdateBranches(repo, CurrentBranch);

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

                    OutputError("Pulled Successfully");
                }
                catch (Exception ex)
                {
                    OutputError(ex.Message);
                }
            });
        }
        #endregion

        #region Clone 

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
        private void ChooseCloneDirectory()
        {
            try
            {
                if (githubUserInfo == null)
                {
                    throw new LoadDataException("No Loaded data!");
                }

                //Open file select dialogue
                OpenFolderDialog dialog = new OpenFolderDialog
                {
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                };

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
                        var remote = repo.Network.Remotes.FirstOrDefault();
                        if (remote != null)
                        {
                            RepoURL = remote.Url;
                        }
                        else
                        {
                            throw new NullReferenceException("Couldn't find any remotes!");
                        }

                        //Save to user info
                        githubUserInfo.SavedRepository = new(LocalRepoFilePath, RepoURL);
                        JsonDataManager.SaveUserInfo(githubUserInfo);
                        
                        //Set flags
                        CanClone = true;
                        DirectoryHasFiles = false;
                        NoRepoCloned = false;
                        
                        ResetBranches();
                        UpdateBranches(repo, new GitBranch());
                    }
                }
            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
        #endregion

        #region Dialog Commands
        [RelayCommand]
        public void OpenCloneWindow()
        {
            _dialogService.ShowCloneRepoDialog();
        }
        [RelayCommand]
        public void OpenRemoteWindow()
        {
            _dialogService.ShowRemoteBranchesDialog();
        }

        [RelayCommand]
        public void OpenTutorialPage()
        {
            IsTutorialOpen = !IsTutorialOpen;
        }

        [RelayCommand]
        public void OpenSettingsWindow()
        {
            _dialogService.ShowSettingsDialog();
        }

        [RelayCommand]
        public void OpenConsoleWindow()
        {
            _dialogService.ShowConsoleDialog();
        }
        #endregion

        #region Helper Methods
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
            catch (Exception ex)
            {
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
        /// Resets collection of local branches and re-initializes the lists
        /// </summary>
        public void ResetBranches()
        {
            LocalBranches.Clear();
            RemoteBranches.Clear();
        }
        #endregion
    }
}
