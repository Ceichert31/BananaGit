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
        await _gitService.PushFiles();
    }

    /// <summary>
    /// Calls git service to commit locally staged files
    /// </summary>
    [RelayCommand]
    private async Task CommitStagedFiles()
    {
        if (!_gitService.HasLocalChanges()) return;
        
        await _gitService.CommitStagedFilesAsync(CommitMessage);
    }
}