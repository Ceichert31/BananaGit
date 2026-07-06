using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using BananaGit.EventArgExtensions;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;

namespace BananaGit.ViewModels.DialogueViewModels;

partial class CreateBranchViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<GitBranch>? _localBranches;

    [NotifyCanExecuteChangedFor(nameof(CreateBranchCommand))] [ObservableProperty]
    private GitBranch? _selectedBranch;

    [ObservableProperty] private string _branchName = string.Empty;

    [ObservableProperty] private string _message = string.Empty;

    private readonly GitService _gitService;
    private readonly DialogService _dialogService;

    public CreateBranchViewModel(GitService gitService, DialogService dialogService)
    {
        _gitService = gitService;
        _dialogService = dialogService;
        gitService.OnChangesPulled += OnChangesPulled;
        LocalBranches = new ObservableCollection<GitBranch>(_gitService.GetLocalBranches());
    }

    /// <summary>
    /// Creates a new branch based off an existing branch
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCreateBranch))]
    private async Task CreateBranch()
    {
        try
        {
            // This is guaranteed to not be null because this command can only be 
            // executed when SelectedBranch is not null. 
            await _gitService.CreateBranchAsync(SelectedBranch!, BranchName);

            await _gitService.PullChanges();
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
        catch (LibGit2SharpException ex)
        {
            Message = "An error occured when trying to create branch. Check Console and try again.";
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }

        SelectedBranch = null;
        _dialogService.CloseCreateBranchDialog();
    }

    private bool CanCreateBranch() => SelectedBranch != null;

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