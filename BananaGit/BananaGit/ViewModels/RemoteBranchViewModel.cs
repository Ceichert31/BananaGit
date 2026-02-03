using System.Collections.ObjectModel;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels;

partial class RemoteBranchViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<GitBranch> _remoteBranches = new();
    
    private readonly GitService _gitService;

    public RemoteBranchViewModel(GitService gitService)
    {
        _gitService = gitService;

        _gitService.OnRepositoryChanged += UpdateRemoteBranches;
        _gitService.OnChangesPulled += UpdateRemoteBranches;
    }

    /// <summary>
    /// Updates the remote branches dialog
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void UpdateRemoteBranches(object? sender, EventArgs e)
    {
        RemoteBranches.Clear();
        RemoteBranches = new(_gitService.GetRemoteBranches());
    }
}