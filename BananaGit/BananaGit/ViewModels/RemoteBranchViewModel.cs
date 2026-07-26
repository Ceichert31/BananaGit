using System.Collections.ObjectModel;
using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels;

partial class RemoteBranchViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<GitBranch> _remoteBranches = new();

    [ObservableProperty] private bool _showBranchOptions;

    private readonly GitService _gitService;

    public RemoteBranchViewModel(GitService gitService)
    {
        _gitService = gitService;

        UpdateRemoteBranches(this, EventArgs.Empty);

        _gitService.OnRepositoryChanged += UpdateRemoteBranches;
        _gitService.OnChangesPulled += UpdateRemoteBranches;
    }

    /// <summary>
    /// Updates the remote branches dialog
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void UpdateRemoteBranches(object? sender, EventArgs e)
    {
        try
        {
            var branches = await _gitService.GetRemoteBranchesAsync();

            RemoteBranches.Clear();
            foreach (var branch in branches)
                RemoteBranches.Add(branch);
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}