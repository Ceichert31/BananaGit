using System.Collections.ObjectModel;
using System.Windows;
using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

/// <summary>
/// View model for Toolbar view
/// </summary>
partial class ToolbarViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isTutorialOpen;

    [ObservableProperty]
    private TerminalViewModel _terminalViewModel = new();
    
    [ObservableProperty]
    private ObservableCollection<GitBranch> _localBranches = [];

    public GitBranch? CurrentBranch
    {
        get => _gitInfo.CurrentBranch;
        set => _gitInfo.CurrentBranch = value;
    }
    
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
        try
        {
            await _gitService.PushFiles();
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    [RelayCommand]
    private async Task CallPullFiles()
    {
        try
        {
            await _gitService.PullChanges();
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
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
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //Cache current branch name
                string? currentBranchName = CurrentBranch?.Name;

                LocalBranches.Clear();

                var branches = _gitService.GetLocalBranches();

                //Re-add all branches
                foreach (var branch in branches)
                {
                    LocalBranches.Add(branch);
                }

                //Find the current branch and cache it
                if (!string.IsNullOrEmpty(currentBranchName))
                {
                    CurrentBranch = LocalBranches.FirstOrDefault(n => n.Name == currentBranchName);
                }

                //If current branch couldn't be found, set current branch to first branch
                if (CurrentBranch == null && LocalBranches.Any())
                {
                    CurrentBranch = LocalBranches.First();
                }

                OnPropertyChanged(nameof(CurrentBranch));
            });
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}