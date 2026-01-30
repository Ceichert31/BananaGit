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
    
    private readonly DialogService? _dialogService;
    private readonly GitService? _gitService;

    public ToolbarViewModel(DialogService dialogService, GitService gitService)
    {
        _dialogService = dialogService;
        _gitService = gitService;
    }
    
    [RelayCommand]
    private void OpenTutorial()
    {
        IsTutorialOpen = !IsTutorialOpen;
    }
    [RelayCommand]
    private void OpenCloneWindow()
    {
        _dialogService?.ShowCloneRepoDialog();
    }
    [RelayCommand]
    private void OpenRemoteWindow()
    {
        _dialogService?.ShowRemoteBranchesDialog();
    }
    [RelayCommand]
    private void OpenSettingsWindow()
    {
        _dialogService?.ShowSettingsDialog();
    }

    [RelayCommand]
    private void OpenConsoleWindow()
    {
        _dialogService?.ShowConsoleDialog(TerminalViewModel);
    }

    [RelayCommand]
    private async Task CallPushFiles()
    {
        if (_gitService == null) return;
        await _gitService.PushFiles();
    }

    [RelayCommand]
    private async Task CallPullFiles()
    {
        if (_gitService == null) return;
        await _gitService.PullChanges();
    }
}