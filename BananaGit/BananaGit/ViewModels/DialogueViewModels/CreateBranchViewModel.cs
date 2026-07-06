using System.Collections.ObjectModel;
using System.Diagnostics;
using BananaGit.EventArgExtensions;
using BananaGit.Exceptions;
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

    [ObservableProperty] private string _message = string.Empty;

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
    private async Task CreateBranch()
    {
        if (SelectedBranch == null)
        {
            Message = "Please select a branch";
            return;
        }

        try
        {
            _gitService.CreateBranch(SelectedBranch, BranchName);

            await _gitService.PushFiles();
        }
        catch (InvalidOperationException ex)
        {
            Message = "Couldn't find selected branch.";
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
        catch (InvalidBranchException ex)
        {
            Message = "An invalid branch was selected. Please try again.";
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    /// <summary>
    /// Called when changes are pulled
    /// </summary>
    /// <param name="sender"><see cref="GitService"/></param>
    /// <param name="e">Empty</param>
    private void OnChangesPulled(object? sender, EventArgs e)
    {
        var selectedName = SelectedBranch?.Name;
        LocalBranches = new ObservableCollection<GitBranch>(_gitService.GetLocalBranches());
        SelectedBranch = LocalBranches.FirstOrDefault(x => string.Equals(x.Name, selectedName));
    }
}