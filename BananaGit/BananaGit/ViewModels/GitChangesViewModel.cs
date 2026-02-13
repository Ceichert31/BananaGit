using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using BananaGit.EventArgExtensions;
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
    [ObservableProperty]
    private ObservableCollection<ChangedFile> _currentChanges = [];
    [ObservableProperty]
    private ObservableCollection<ChangedFile> _stagedChanges = [];

    private readonly GitService _gitService;
    private readonly DispatcherTimer _updateGitInfoTimer = new();
    
    private const float UpdateGitInfoInterval = 1000f;
    
    public GitChangesViewModel(GitService gitService)
    {
        _gitService = gitService;
        
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
        try
        {
            CurrentChanges = new ObservableCollection<ChangedFile>(_gitService.GetUnstagedChanges());
            StagedChanges = new ObservableCollection<ChangedFile>(_gitService.GetStagedChanges());
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    /// <summary>
    /// Calls git service to discard all local changes (Not including local commits)
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
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
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
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
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
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}