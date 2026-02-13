using System.Diagnostics;
using BananaGit.EventArgExtensions;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

/// <summary>
/// View model for the commit view 
/// </summary>
partial class CommitViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _hasCommitedFiles;
    
    [ObservableProperty]
    private string _commitMessage = string.Empty;
    
    [ObservableProperty]
    private string _selectedCommitHeader = string.Empty;
    
    private readonly GitService _gitService;
    
    public CommitViewModel(GitService gitService)
    {
        _gitService = gitService;
    }

    /// <summary>
    /// Calls git service to push locally commited files
    /// </summary>
    [RelayCommand]
    private async Task PushFiles()
    {
        try
        {
            await _gitService.PushFiles();

            HasCommitedFiles = false;
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    /// <summary>
    /// Calls git service to commit locally staged files
    /// </summary>
    [RelayCommand]
    private async Task CommitStagedFiles()
    {
        try
        {
            if (!_gitService.HasLocalChanges()) return;

            await _gitService.CommitStagedFilesAsync($"{SelectedCommitHeader}: {CommitMessage}");

            HasCommitedFiles = true;

            //Clear commit message
            CommitMessage = string.Empty;
            SelectedCommitHeader = string.Empty;
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}