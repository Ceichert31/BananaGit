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
    private readonly GitDialogService _dialogService;

    public CreateBranchViewModel(GitService gitService, GitDialogService dialogService)
    {
        _gitService = gitService;
        _dialogService = dialogService;
        gitService.OnChangesPulled += OnChangesPulled;

        UpdateLocalBranches();
    }

    /// <summary>
    /// Gets the local branches and updates them
    /// </summary>
    private async void UpdateLocalBranches()
    {
        try
        {
            List<GitBranch>? branches = null;

            await Task.Run(async () => { branches = await _gitService.GetLocalBranchesAsync(); });

            if (branches == null)
                throw new NullReferenceException("No local branches found!");

            LocalBranches = new ObservableCollection<GitBranch>(branches);
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    /// <summary>
    /// Creates a new branch based off an existing branch
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCreateBranch))]
    private async Task CreateBranch()
    {
        try
        {
            _dialogService.CloseCreateBranchDialog();

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
        UpdateLocalBranches();
        SelectedBranch = LocalBranches?.FirstOrDefault(x => string.Equals(x.Name, selectedName));
    }
}