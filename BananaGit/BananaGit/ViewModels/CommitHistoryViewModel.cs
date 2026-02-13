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
    [ObservableProperty]
    private string _repositoryName = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<GitCommitInfo> _commitHistory = [];
    
    private readonly GitService _gitService;
    
    private int _maxCommitHistoryLength = 30;

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
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
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
            CommitHistory = new(_gitService.GetCommitHistory(_maxCommitHistoryLength));
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
}