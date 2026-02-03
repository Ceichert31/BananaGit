using System.Collections.ObjectModel;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

partial class ToolbarViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isTutorialOpen;

    [ObservableProperty]
    private TerminalViewModel _terminalViewModel = new();
    
    [ObservableProperty]
    private bool _hasCommitedFiles;
    
    [ObservableProperty]
    private ObservableCollection<GitBranch> _localBranches = [];


    
    public GitBranch? CurrentBranch => _gitInfo.CurrentBranch;
    
    private readonly DialogService _dialogService;
    private readonly GitService _gitService;
    private readonly GitInfoModel _gitInfo;

    public ToolbarViewModel(DialogService dialogService, GitService gitService, GitInfoModel gitInfo)
    {
        _dialogService = dialogService;
        _gitService = gitService;
        _gitInfo = gitInfo;
    }
    
    [RelayCommand]
    private void OpenTutorial()
    {
        IsTutorialOpen = !IsTutorialOpen;
    }
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
    private void OpenSettingsWindow()
    {
        _dialogService.ShowSettingsDialog();
    }

    [RelayCommand]
    private void OpenConsoleWindow()
    {
        _dialogService.ShowConsoleDialog(TerminalViewModel);
    }

    [RelayCommand]
    private async Task CallPushFiles()
    {
        await _gitService.PushFiles();
        HasCommitedFiles = false;
    }

    [RelayCommand]
    private async Task CallPullFiles()
    {
        await _gitService.PullChanges();
        UpdateBranches();
    }
    
    /// <summary>
    /// Checks if any new branches were added and adds them to list
    /// </summary>
    private void UpdateBranches()
    { 
        LocalBranches.Clear(); 
        LocalBranches = new(_gitService.GetLocalBranches());
    }
     
      
        /*
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
        */
      
        /*
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
                DirectoryHasFiles = true;#1#
                NoRepoCloned = false;
                        
                ResetBranches();
                UpdateBranches(new GitBranch(githubUserInfo));
            });
        }
        */
        
        /*/// <summary>
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
                await _gitService?.CloneRepositoryAsync(RepoURL,
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
        }*/

}