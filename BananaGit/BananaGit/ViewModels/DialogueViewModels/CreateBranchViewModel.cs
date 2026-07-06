using System.Collections.ObjectModel;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels.DialogueViewModels;

public partial class CreateBranchViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<GitBranch>? _localBranches;

    private readonly GitService _gitService;

    public CreateBranchViewModel(GitService gitService)
    {
        _gitService = gitService;
        gitService.OnChangesPulled += OnChangesPulled;
        LocalBranches = new ObservableCollection<GitBranch>(_gitService.GetLocalBranches());
    }

    /// <summary>
    /// Called when changes are pulled
    /// </summary>
    /// <param name="sender"><see cref="GitService"/></param>
    /// <param name="e">Empty</param>
    private void OnChangesPulled(object? sender, EventArgs e)
    {
        LocalBranches = new ObservableCollection<GitBranch>(_gitService.GetLocalBranches());
    }
}