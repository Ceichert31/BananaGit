using System.Diagnostics;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;

namespace BananaGit.ViewModels;

partial class TutorialViewModel(DialogService dialogService, GitService gitService) : ObservableObject
{
    [ObservableProperty]
    private bool _isTutorialOpen;

    [ObservableProperty]
    private TerminalViewModel _terminalViewModel = new();
    
    private readonly DialogService _dialogService = dialogService;
    private readonly GitService _gitService = gitService;

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
     /*/// <summary>
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
                    
                HasCommitedFiles = false;
            }
            catch (LibGit2SharpException ex)
            {
                Trace.WriteLine($"Failed to Push {ex.Message}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
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
                        Trace.WriteLine("Conflict detected");
                        return;
                    case MergeStatus.UpToDate:
                        //Display in front end eventually
                        Trace.WriteLine("Up to date");
                        return;
                    case MergeStatus.FastForward:
                        Trace.WriteLine("Fast Forward");
                        break;
                    case MergeStatus.NonFastForward:
                        Trace.WriteLine("Non-Fast Forward");
                        break;
                    default:
                        Trace.WriteLine("Pulled Successfully");
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }*/
}