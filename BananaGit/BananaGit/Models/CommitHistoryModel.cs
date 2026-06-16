using BananaGit.EventArgExtensions;
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
    private readonly List<GitCommitInfo>? _commitHistory;

    public CommitHistoryPage(GitService gitService, int historyLength, uint pageIndex,
        ref EventHandler<PageNumberEventArgs>? onPageChanged)
    {
        _commitHistory = new List<GitCommitInfo>(historyLength);
        onPageChanged += OnPageChanged;

        //Realized this won't work because we aren't tracking previous history lengths. We need to be able to get a range
        //If we have the index of the current page and the length, then we can figure out what part we need

        // min = index * historyLength - historyLength

        // 2 * 30 - 30
        // 60 - 30
        // 30

        //max = index * historyLength
        // 2 * 30 = 60

        //So for page to we would need the range of 30-60

        //Calculate min and max
        uint min = (uint)(pageIndex * historyLength - historyLength);
        uint max = (uint)(pageIndex * historyLength);

        //Load specified length of history
        _commitHistory = gitService.GetCommitHistoryRange(min, max);
    }

    /// <summary>
    /// Called when the user switches pages 
    /// </summary>
    /// <param name="sender"><see cref="CommitHistoryViewModel"/></param>
    /// <param name="e">Empty</param>
    private void OnPageChanged(object? sender, PageNumberEventArgs e)
    {
        //
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