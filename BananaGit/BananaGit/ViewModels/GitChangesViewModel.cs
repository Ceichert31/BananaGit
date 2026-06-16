using System.Collections.ObjectModel;
using System.Windows.Threading;
using BananaGit.EventArgExtensions;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

/// <summary>
/// View model for git changes view
/// </summary>
partial class GitChangesViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ChangedFile> _currentChanges = [];
    [ObservableProperty] private ObservableCollection<ChangedFile> _stagedChanges = [];

    private readonly GitService _gitService;
    private readonly DialogService _dialogService;
    private readonly DispatcherTimer _updateGitInfoTimer = new();

    private const float UpdateGitInfoInterval = 1000f;

    public GitChangesViewModel(GitService gitService, DialogService dialogService)
    {
        _gitService = gitService;
        _dialogService = dialogService;

        _updateGitInfoTimer.Tick += UpdateLocalRepositoryChanges;
        _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(UpdateGitInfoInterval);
        _updateGitInfoTimer.Start();
    }

    /// <summary>
    /// Updates the locally modified files 
    /// </summary>
    /// <param name="sender">Dispatch timer</param>
    /// <param name="e">Empty</param>
    private void UpdateLocalRepositoryChanges(object? sender, EventArgs e)
    {
        if (!_gitService.IsLocalRepositoryOpen())
            return;

        try
        {
            CurrentChanges = new ObservableCollection<ChangedFile>(_gitService.GetUnstagedChanges());
            StagedChanges = new ObservableCollection<ChangedFile>(_gitService.GetStagedChanges());
        }
        catch (RepoLocationException ex)
        {
            GitService.OutputToConsole(this,
                new MessageEventArgs($"Repository is open but could not be found! {ex.Message}"));
        }
    }

    /// <summary>
    /// Calls git service to discard all local changes (Not including local commits)
    /// </summary>
    [RelayCommand]
    private void CallDiscardLocalChanges()
    {
        try
        {
            //Import dialog service with DI so we can open dialog
            //Create pop up that shows up to confirm reset
            //await _gitService.ResetLocalUncommittedFilesAsync();
            _dialogService.ShowDiscardChangesDialog();
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    /// <summary>
    /// Calls git service to unstage all uncommited local changes
    /// </summary>
    [RelayCommand]
    private async Task UnstageLocalChanges()
    {
        try
        {
            await _gitService.UnstageFilesAsync();
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    /// <summary>
    /// Calls git service to stage all uncommited local changes
    /// </summary>
    [RelayCommand]
    private async Task StageLocalChanges()
    {
        try
        {
            await _gitService.StageFilesAsync();
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}