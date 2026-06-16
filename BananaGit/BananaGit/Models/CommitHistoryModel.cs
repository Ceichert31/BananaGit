using System.Collections.ObjectModel;
using BananaGit.EventArgExtensions;
using BananaGit.Exceptions;
using BananaGit.Services;
using BananaGit.ViewModels;

namespace BananaGit.Models;

//Maybe I should be passing GitService by reference? Each time I use DI I might be 
//Creating a copy of it and the GitInfo inside of it, which would take up alot of memory

/// <summary>
/// Holds information on a set amount of previous commits
/// </summary>
public class CommitHistoryPage
{
    public ObservableCollection<GitCommitInfo>? CommitHistoryList => _commitHistory;

    private readonly ObservableCollection<GitCommitInfo>? _commitHistory;

    private readonly uint _pageIndex;

    public CommitHistoryPage(GitService gitService, int historyLength, uint pageIndex,
        ref EventHandler<PageNumberEventArgs>? onPageChanged)
    {
        _commitHistory = new ObservableCollection<GitCommitInfo>();
        _pageIndex = pageIndex;
        onPageChanged += OnPageChanged;

        //Calculate min and max
        uint min = 0;
        uint max = (uint)historyLength;

        //Index 0 edge case
        if (pageIndex != 0)
        {
            min = (uint)(pageIndex * historyLength - historyLength);
            max = (uint)(pageIndex * historyLength);
        }

        if (!gitService.IsLocalRepositoryOpen())
            return;

        try
        {
            //Load specified length of history
            _commitHistory = new ObservableCollection<GitCommitInfo>(gitService.GetCommitHistoryRange(min, max));
        }
        catch (RepoLocationException)
        {
            GitService.OutputToConsole(this, new MessageEventArgs("Failed to load commit history"));
        }
    }

    /// <summary>
    /// Called when the user switches pages 
    /// </summary>
    /// <param name="sender"><see cref="CommitHistoryViewModel"/></param>
    /// <param name="e">Empty</param>
    private void OnPageChanged(object? sender, PageNumberEventArgs e)
    {
        /*if (_pageIndex - e.PageNumber > 1)
        {
        }*/
    }
}

/// <summary>
/// Contains information on previous commits in the repository
/// </summary>
public class GitCommitInfo
{
    public string? Author { get; set; }
    public string? Date { get; set; }
    public string? Message { get; set; }
    public string? Commit { get; set; }
    public bool IsMergeCommit { get; set; }
}