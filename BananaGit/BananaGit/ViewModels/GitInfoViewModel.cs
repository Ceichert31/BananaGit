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
        private readonly GitService _gitService;

        public GitInfoViewModel(GitService gitService)
        {
            _dialogService = new DialogService(this,gitService);
            _gitService = gitService;
            
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
                LocalRepoFilePath = githubUserInfo?.GetPath() ?? throw  new NullReferenceException("Repo path is null");
                RepoURL = githubUserInfo?.GetPath() ?? throw new NullReferenceException("Repo URL is null");

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
                        Commit = item.Id.ToString(),
                        //Check if more than one parent, then it is a merge commit
                        IsMergeCommit = item.Parents.Count() > 1
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
        private void UpdateBranches(GitBranch currentBranch)
        {
            try
            {
                LocalBranches.Add(currentBranch);

                //Set fetch options to prune any old remote branches
                var fetchOptions = new FetchOptions { Prune = true };

                VerifyPath(LocalRepoFilePath);

                using var repo = new Repository(LocalRepoFilePath);

                //Cache first remote
                var remote = repo.Network.Remotes.FirstOrDefault();

                //Prune remote branches
                if (remote != null)
                {
                    Commands.Fetch(repo, remote.Name, Array.Empty<string>(), fetchOptions, "");
                }
                else
                {
                    throw new NullReferenceException("Couldn't prune remote branches!");
                }

                //Update branch data on a separate thread
                Application.Current.Dispatcher.Invoke((() =>
                {
                    RepoName = new DirectoryInfo(githubUserInfo.GetPath()).Name ?? "N/A";
                    
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
                            if (RemoteBranches.Any(x => x.Branch == branch))
                                continue;

                            RemoteBranches.Add(new GitBranch(branch));
                            continue;
                        }

                        //Check if local branch already exists
                        if (LocalBranches.Any(x => x.Branch == branch))
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

        #endregion

        #region Stage/Commit
        /// <summary>
        /// Calls GitService to commit staged files and handles any errors
        /// </summary>
        [RelayCommand]
        private void CommitStagedFiles()
        {
            try
            {
                using var repo = new Repository(githubUserInfo?.GetPath());

                var staged = repo.RetrieveStatus().Staged;
                var added = repo.RetrieveStatus().Added;
                var removed = repo.RetrieveStatus().Removed;
                
                if (!staged.Any() && !added.Any() && !removed.Any()) return;
                
                //Discard return value
                _ = _gitService.CommitStagedFilesAsync($"{SelectedCommitHeader} {CommitMessage}");
                
                SelectedCommitHeader = string.Empty;
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

        /// <summary>
        /// Calls GitService to stage all changed files, handles any errors
        /// </summary>
        [RelayCommand]
        private void StageFiles()
        {
            Task.Run(() =>
            {
                try
                {
                    _gitService.StageFilesAsync();
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

        /// <summary>
        /// Calls GitService to unstage all staged files, handles any errors
        /// </summary>
        [RelayCommand]
        private void UnstageFiles()
        {
            Task.Run(() =>
            {
                try
                {
                    _gitService.UnstageFilesAsync();
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

        /// <summary>
        /// Calls GitService to stage a specific file, handles any errors
        /// </summary>
        /// <param name="file">The file to stage</param>
        [RelayCommand]
        private void StageFile(ChangedFile file)
        {
            try
            {
                _gitService.StageFileAsync(file);
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to stage {ex.Message}");
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }

        /// <summary>
        /// Calls GitService to unstage a specific file, handles any errors
        /// </summary>
        /// <param name="file">The file to unstage</param>
        [RelayCommand]
        private void UnstageFile(ChangedFile file)
        {
            try
            {
                _gitService.UnstageFileAsync(file);
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to stage {ex.Message}");
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
        private void PushFiles()
        {
            Task.Run(() =>
            {
                try
                {
                    if (CurrentBranch == null) 
                        throw new NullReferenceException("No Branch selected! Branch is null!");
                    
                    _gitService.PushFilesAsync(CurrentBranch);
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
        private async void PullChanges()
        {
            Task.Run(() => {
                try
                {
                    if (CurrentBranch == null)
                        throw new NullReferenceException("No Branch selected! Branch is null!");
                    
                    MergeStatus status = _gitService.PullFilesAsync(CurrentBranch).Result;
                   
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
            });
        }
        #endregion

        #region Clone 

        /// <summary>
        /// Calls GitService to clone repo, handles any errors
        /// </summary>
        /// <exception cref="LoadDataException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        [RelayCommand]
        private void CloneRepo()
        {
            try
            {
                //Clone repo using git service
                _gitService.CloneRepositoryAsync(RepoURL,
                    LocalRepoFilePath);

                //Open after cloning
                OpenLocalRepository(LocalRepoFilePath);
            }
            catch (LibGit2SharpException ex)
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
                githubUserInfo.SavedRepository = new SavableRepository(LocalRepoFilePath, RepoURL);
                JsonDataManager.SaveUserInfo(githubUserInfo);
            
                //Set flags
                CanClone = true;
                DirectoryHasFiles = false;
                NoRepoCloned = false;
                        
                ResetBranches();
                UpdateBranches(new GitBranch());
            });
        }
        #endregion

        #region Dialog Commands
        [RelayCommand]
        private void OpenCloneWindow()
        {
            _dialogService.ShowCloneRepoDialog();
        }
        [RelayCommand]
        private void OpenRemoteWindow()
        {
            _dialogService.ShowRemoteBranchesDialog();
        }

        [RelayCommand]
        private void OpenTutorialPage()
        {
            IsTutorialOpen = !IsTutorialOpen;
        }

        [RelayCommand]
        private void OpenSettingsWindow()
        {
            _dialogService.ShowSettingsDialog();
        }

        [RelayCommand]
        private void OpenConsoleWindow()
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
