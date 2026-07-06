using System.Collections.ObjectModel;
using System.Diagnostics;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels.DialogueViewModels;

public partial class CreateBranchViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<GitBranch>? _localBranches;

    [ObservableProperty] private GitBranch? _selectedBranch;

    [ObservableProperty] private string _branchName = string.Empty;

    private readonly GitService _gitService;

    public CreateBranchViewModel(GitService gitService)
    {
        _gitService = gitService;
        gitService.OnChangesPulled += OnChangesPulled;
        LocalBranches = new ObservableCollection<GitBranch>(_gitService.GetLocalBranches());
    }

    /// <summary>
    /// Creates a new branch based off an existing branch
    /// </summary>
    [RelayCommand]
    private void CreateBranch()
    {
        var temp = SelectedBranch?.Name;
        Trace.WriteLine(temp);
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