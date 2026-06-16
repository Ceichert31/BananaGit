using System.Collections.ObjectModel;
using System.Diagnostics;
using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels;

/// <summary>
/// View model for the commit history view
/// </summary>
partial class CommitHistoryViewModel : ObservableObject
{
    [ObservableProperty] private string _repositoryName = string.Empty;

    [ObservableProperty] private ObservableCollection<GitCommitInfo> _commitHistory = [];

    public bool IsLocalRepositoryOpen => !_gitService.IsLocalRepositoryOpen();

    private readonly GitService _gitService;

    private const int HistoryLengthPerPage = 30;

    //Create a page model that contains a list of 30 commits
    //Create a list of page models in here
    //Unallocate page model if it is 2 pages away from the current index
    // Current: 3
    // 1 - 2 - 3
    //Current: 1
    // 1 - 2
    //Maybe have an on page change event that triggers that each page subscribes to
    //That way when the page number changes each page checks internally whether it should unallocate or not

    public CommitHistoryViewModel(GitService gitService)
    {
        _gitService = gitService;
        _gitService.OnRepositoryChanged += RepositoryChanged;
        _gitService.OnChangesPulled += PulledChanges;
    }

    /// <summary>
    /// Called when a new repository is cloned or opened
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RepositoryChanged(object? sender, EventArgs e)
    {
        try
        {
            //Display new repo name
            RepositoryName = _gitService.GetRepositoryName();
            PulledChanges(sender, e);
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    /// <summary>
    /// Called when the user pulls changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PulledChanges(object? sender, EventArgs e)
    {
        try
        {
            CommitHistory = new(_gitService.GetCommitHistory(HistoryLengthPerPage));
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}