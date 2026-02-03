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
        private GitBranch? _currentBranch;



        //Flags
        [ObservableProperty]
        private bool _canClone = false;

        [ObservableProperty]
        private bool _directoryHasFiles = false;

        [ObservableProperty]
        private bool _noRepoCloned;

     
        #endregion

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

                //UpdateBranches(CurrentBranch);
                _gitService.OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
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
                //VerifyPath(LocalRepoFilePath);

                //Refactor commit history to not update constantly
                CurrentChanges.Clear();
                StagedChanges.Clear();

                using var repo = new Repository(LocalRepoFilePath);
                var stats = repo.RetrieveStatus(new StatusOptions());

                //Update list of all commits
                if (CurrentBranch == null || CurrentBranch.Name == "") throw new NullReferenceException("Current branch isn't set");

                var currentBranch = repo.Branches[CurrentBranch.Name] ?? throw new NullReferenceException("Current branch isn't set");

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
        private async Task CallPushFiles()
        { 
            await _gitService.PushFiles();
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
        /// Throws errors on the main thread
        /// </summary>
        /// <param name="message"></param>
        private static void OutputError(string message)
        {
            Application.Current.Dispatcher.Invoke(() => { Trace.WriteLine(message); });
        }

        /*
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
        }*/
        #endregion
    }
}
