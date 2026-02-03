using System.Collections.ObjectModel;
using System.Windows;
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
        
        _gitService.OnRepositoryChanged += UpdateBranches;
        _gitService.OnChangesPulled += UpdateBranches;
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
    }

    [RelayCommand]
    private void CallRepositoryDialog()
    {
        _gitService.ChooseRepositoryDialog();
    }
    
    /// <summary>
    /// Checks if any new branches were added and adds them to list
    /// </summary>
    private void UpdateBranches(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LocalBranches.Clear(); 
            LocalBranches = new(_gitService.GetLocalBranches());
        });
    }
        
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