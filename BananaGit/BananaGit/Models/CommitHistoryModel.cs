using BananaGit.Services;

namespace BananaGit.Models;

//Maybe I should be passing GitService by reference? Each time I use DI I might be 
//Creating a copy of it and the GitInfo inside of it, which would take up alot of memory

/// <summary>
/// Holds information on a set amount of previous commits
/// </summary>
public class CommitHistoryPage
{
    private readonly List<GitCommitInfo>? _commitHistory;

    private readonly GitService _gitService;

    public CommitHistoryPage(GitService gitService, int historyLength)
    {
        _gitService = gitService;
        _commitHistory = new List<GitCommitInfo>(historyLength);
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