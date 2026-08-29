using System.Collections.ObjectModel;
using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels;

partial class RemoteBranchViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<GitBranch> _remoteBranches = new();
    [ObservableProperty] private ObservableCollection<GitBranch> _localBranches = new();

    [ObservableProperty] private bool _showBranchOptions;

    private readonly GitService _gitService;

    public RemoteBranchViewModel(GitService gitService)
    {
        _gitService = gitService;

        UpdateBranches(this, EventArgs.Empty);

        _gitService.OnRepositoryChanged += UpdateBranches;
        _gitService.OnChangesPulled += UpdateBranches;
    }

    /// <summary>
    /// Updates both the local and remote branches in the remote branch view
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void UpdateBranches(object? sender, EventArgs e)
    {
        try
        {
            List<GitBranch>? remoteBranches = null;
            List<GitBranch>? localBranches = null;

            await Task.Run(async () => { remoteBranches = await _gitService.GetRemoteBranchesAsync(); });
            await Task.Run(async () => { localBranches = await _gitService.GetLocalBranchesAsync(); });

            if (remoteBranches == null)
                throw new NullReferenceException("No remote branches found");

            RemoteBranches.Clear();
            foreach (var branch in remoteBranches)
                RemoteBranches.Add(branch);

            if (localBranches == null)
                throw new NullReferenceException("No local branches found");

            LocalBranches.Clear();
            foreach (var branch in localBranches)
                LocalBranches.Add(branch);
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}