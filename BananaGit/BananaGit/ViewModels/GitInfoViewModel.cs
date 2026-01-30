using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;
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

     
        #endregion

        private int _maxCommitHistoryLength = 30;

        private readonly DispatcherTimer _updateGitInfoTimer = new();

        private GitInfoModel? githubUserInfo;

        private readonly DialogService _dialogService;
        private readonly GitService _gitService;

        public GitInfoViewModel(GitService gitService, GitInfoModel userInfo)
        {
            _dialogService = new DialogService(this);
            _gitService = gitService;
            
            _updateGitInfoTimer.Tick += UpdateRepoStatus;
            _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _updateGitInfoTimer.Start();

            githubUserInfo = userInfo;

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
                CurrentBranch = new GitBranch(githubUserInfo);

                //Load current repo data if there is an already opened repo
                LocalRepoFilePath = githubUserInfo?.GetPath() ?? throw  new NullReferenceException("Repo path is null");
                RepoURL = githubUserInfo?.GetUrl() ?? throw new NullReferenceException("Repo URL is null");

                //Update that we successfully initialized the repository
                NoRepoCloned = false;

                UpdateBranches(CurrentBranch);
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
                githubUserInfo.SavedRepository.Url = RepoURL;
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
        private void UpdateRepoStatus(object? sender, EventArgs e)
        {
            if (NoRepoCloned) return;
            try
            {
                VerifyPath(LocalRepoFilePath);

                //Refactor commit history to not update constantly
                CurrentChanges.Clear();
                StagedChanges.Clear();

                using var repo = new Repository(LocalRepoFilePath);
                var stats = repo.RetrieveStatus(new StatusOptions());

                CommitHistory.Clear();
                //Update list of all commits
                if (CurrentBranch == null || CurrentBranch.Name == "") throw new NullReferenceException("Current branch isn't set");

                var currentBranch = repo.Branches[CurrentBranch.Name] ?? throw new NullReferenceException("Current branch isn't set");

                var commits = currentBranch.Commits.ToList();

                int commitCount = 0;
                foreach (var commit in commits)
                {
                    //Break out if commit history is too long
                    if (commitCount > _maxCommitHistoryLength)
                        break;
                    commitCount++;
                    
                    GitCommitInfo commitInfo = new()
                    {
                        Author = commit.Author.ToString(),
                        Date =
                            $"{commit.Author.When.DateTime.ToShortTimeString()} {commit.Author.When.DateTime.ToShortDateString()}",
                        Message = commit.Message,
                        Commit = commit.Id.ToString(),
                        //Check if more than one parent, then it is a merge commit
                        IsMergeCommit = commit.Parents.Count() > 1
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
                        CurrentChanges.Add(new(_gitService, file, file.FilePath));
                    }
                    //Staging logic
                    else if (file.State == FileStatus.ModifiedInIndex || file.State == FileStatus.NewInIndex
                        || file.State == FileStatus.RenamedInIndex || file.State == FileStatus.DeletedFromIndex)
                    {
                        StagedChanges.Add(new(_gitService, file, file.FilePath));
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
        /// <param name="currentBranch">The currently selected git branch</param>
        private void UpdateBranches(GitBranch currentBranch)
        {
            try
            {
                VerifyPath(LocalRepoFilePath);
                
                //Update UI properties on the UI thread
                Application.Current.Dispatcher.Invoke((() =>
                {
                    using var repo = new Repository(LocalRepoFilePath);
                    
                    LocalBranches.Clear();
                    RemoteBranches.Clear();
                    CurrentBranch = currentBranch;
                    LocalBranches.Add(currentBranch);
                    
                    if (githubUserInfo?.TryGetPath(out var path) == null)
                    {
                        throw new NullReferenceException("Couldn't access repository path!");
                    }
                    RepoName = new DirectoryInfo(path).Name;
                    
                    //Update branches
                    foreach (var branch in repo.Branches)
                    {
                        if (branch.IsRemote)
                        {
                            //Filter out all remotes with ref in the name
                            if (!branch.CanonicalName.StartsWith("refs/remotes/origin"))
                                continue;

                            RemoteBranches.Add(new GitBranch(branch));
                            continue;
                        }

                        //Check if local branch already exists
                        if (LocalBranches.Any(x => x.CanonicalName == branch.CanonicalName))
                            continue;

                        LocalBranches.Add(new GitBranch(branch));
                    }
                }));
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }

        #endregion

        #region Stage/Commit
        /// <summary>
        /// Calls GitService to commit staged files and handles any errors
        /// </summary>
        [RelayCommand]
        private async Task CommitStagedFiles()
        {
            try
            {
                using var repo = new Repository(githubUserInfo?.GetPath());

                var staged = repo.RetrieveStatus().Staged;
                var added = repo.RetrieveStatus().Added;
                var removed = repo.RetrieveStatus().Removed;
                
                if (!staged.Any() && !added.Any() && !removed.Any()) return;
                
                await _gitService.CommitStagedFilesAsync($"{SelectedCommitHeader} {CommitMessage}");
                
                SelectedCommitHeader = string.Empty;
                //Clear commit message
                CommitMessage = string.Empty;
                //HasCommitedFiles = true;
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

        /// <summary>
        /// Calls GitService to stage all changed files, handles any errors
        /// </summary>
        [RelayCommand]
        private async Task StageFiles()
        {
            try
            {
                await _gitService.StageFilesAsync();
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

        /// <summary>
        /// Calls GitService to unstage all staged files, handles any errors
        /// </summary>
        [RelayCommand]
        private async Task UnstageFiles()
        {
            try
            {
                await _gitService.UnstageFilesAsync();
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
        /// Calls GitService to push commited files onto selected branch, handles errors
        /// </summary>
        [RelayCommand]
        private async Task PushFiles()
        {
            try
            {
                if (CurrentBranch == null) 
                    throw new NullReferenceException("No Branch selected! Branch is null!");
                    
                await _gitService.PushFilesAsync(CurrentBranch);
                    
                //HasCommitedFiles = false;
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to Push {ex.Message}");
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }

        /// <summary>
        /// Pulls changes from the repo and merges them into the local repository
        /// </summary>
        [RelayCommand]
        private async Task PullChanges()
        {
            try
            {
                if (CurrentBranch == null)
                    throw new NullReferenceException("No Branch selected! Branch is null!");
                    
                var status = await _gitService.PullFilesAsync(CurrentBranch);
                   
                //Updates the branch list
                UpdateBranches(CurrentBranch);

                switch (status)
                {
                    //Check for merge conflicts
                    case MergeStatus.Conflicts:
                        //Display in front end eventually
                        OutputError("Conflict detected");
                        return;
                    case MergeStatus.UpToDate:
                        //Display in front end eventually
                        OutputError("Up to date");
                        return;
                    case MergeStatus.FastForward:
                        OutputError("Fast Forward");
                        break;
                    case MergeStatus.NonFastForward:
                        OutputError("Non-Fast Forward");
                        break;
                    default:
                        OutputError("Pulled Successfully");
                        break;
                }
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }
        #endregion

        #region Clone 

        /// <summary>
        /// Calls GitService to clone repo, handles any errors
        /// </summary>
        /// <exception cref="LoadDataException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        [RelayCommand]
        private async Task CloneRepo()
        {
            try
            {
                //Clone repo using git service
                await _gitService.CloneRepositoryAsync(RepoURL,
                    LocalRepoFilePath);

                //Open after cloning
                OpenLocalRepository(LocalRepoFilePath);
            }
            catch (LibGit2SharpException)
            {
                OutputError($"Failed to clone repo {githubUserInfo?.SavedRepository?.Url}");
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
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

                    //Check if directory is empty and mark as cloneable
                    if (!Directory.EnumerateFiles(selectedFilePath).Any())
                    {
                        CanClone = true;
                        LocalRepoFilePath = selectedFilePath;
                        DirectoryHasFiles = false;
                    }
                    //Otherwise open if a repository already exists there
                    else
                    {
                        OpenLocalRepository(selectedFilePath);
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
        /// Opens a saved local repository
        /// </summary>
        /// <param name="filePath">The file path to the local repository</param>
        /// <exception cref="NullReferenceException"></exception>
        private void OpenLocalRepository(string filePath)
        {
            RepoName = new DirectoryInfo(filePath).Name ?? "N/A";
            Task.Run(() =>
            {
                //Check if file location is local repo
                if (!Repository.IsValid(filePath)) 
                    throw new RepositoryNotFoundException($"Repository not found at {filePath}!");
            
                var repo = new Repository(filePath);

                //Set active repo as locally opened repo
                LocalRepoFilePath = filePath;
                var remote = repo.Network.Remotes["origin"];
                if (remote != null)
                {
                    RepoURL = remote.Url;
                }
                else
                {
                    throw new NullReferenceException("Couldn't find any remotes!");
                }

                //Save to user info
                githubUserInfo?.SetPath(LocalRepoFilePath);
                githubUserInfo?.SetUrl(RepoURL);
                JsonDataManager.SaveUserInfo(githubUserInfo);
            
                //Set flags
                /*CanClone = false;
                DirectoryHasFiles = true;*/
                NoRepoCloned = false;
                        
                ResetBranches();
                UpdateBranches(new GitBranch(githubUserInfo));
            });
        }
        #endregion

        /// <summary>
        /// Resets the local repository to the remote
        /// </summary>
        [RelayCommand]
        private async Task DiscardLocalChanges()
        {
            try
            {
                await _gitService.ResetLocalUncommittedFilesAsync();
            }   
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
            
        }

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
        private void ResetBranches()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {      
                LocalBranches.Clear();
                RemoteBranches.Clear();
            });
        }
        #endregion
    }
}
