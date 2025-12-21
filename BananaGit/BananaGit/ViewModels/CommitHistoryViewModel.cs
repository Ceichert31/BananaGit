using System.Collections.ObjectModel;
using System.Windows.Threading;
using BananaGit.Models;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using LibGit2Sharp;

namespace BananaGit.ViewModels;

/// <summary>
/// Interaction logic for CommitHistoryView.xaml
/// </summary>
public partial class CommitHistoryViewModel : ObservableObject
{
    //Only needs to load repo info to display git commit history
    //Doesn't need git service
    //Future feature for moving through pages of commit history
    
    [ObservableProperty]
    private ObservableCollection<GitCommitInfo> _commitHistory = [];
    
    private const int CommitHistoryLength = 50;
    private const int CommitHistoryUpdateTime = 500;
    
    private LoadedRepositoryInfo? _loadedRepositoryInfo;
    
    private readonly DispatcherTimer _updateGitInfoTimer = new();
    
    public CommitHistoryViewModel(LoadedRepositoryInfo loadedRepositoryInfo)
    {
        //How will we update repository info 
        _loadedRepositoryInfo = loadedRepositoryInfo;
        
        loadedRepositoryInfo.OnRepositoryChanged += OnUserInfoChanged;
        
        _updateGitInfoTimer.Tick += UpdateCommitHistory;
        _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(CommitHistoryUpdateTime);
        _updateGitInfoTimer.Start();
    }

    /// <summary>
    /// Updates repository information
    /// </summary>
    /// <param name="sender">The new repository information</param>
    /// <param name="e">Event arguments</param>
    private void OnUserInfoChanged(object? sender, EventArgs e)
    {
        if (sender is not LoadedRepositoryInfo loadedRepositoryInfo) return;
        
        _loadedRepositoryInfo = loadedRepositoryInfo;
        UpdateCommitHistory(_loadedRepositoryInfo, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the commit history every tick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void UpdateCommitHistory(object? sender, EventArgs e)
    {
        //Check if commit needs to be updated or is same

        using var repo = new Repository(_loadedRepositoryInfo?.FilePath);
        
        var branch = repo.Branches[_loadedRepositoryInfo?.CurrentBranch.Name];
        
        var commits = branch.Commits.ToList();

        //Limits commit history to a certain length
        for (int i = 0; i < CommitHistoryLength; ++i)
        {
            var item = commits[i];
            GitCommitInfo commitInfo = new()
            {
                Author = item.Author.ToString(),
                Date =
                    $"{item.Author.When.DateTime.ToShortTimeString()} {item.Author.When.DateTime.ToShortDateString()}",
                Message = item.Message,
                Commit = item.Id.ToString()
            };
            CommitHistory.Add(commitInfo);
        }
    }
}