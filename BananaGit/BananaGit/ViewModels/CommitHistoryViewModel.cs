using System.Collections.ObjectModel;
using BananaGit.Models;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;

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
    
    private LoadedRepositoryInfo? _loadedRepositoryInfo;
    
    public CommitHistoryViewModel(LoadedRepositoryInfo loadedRepositoryInfo)
    {
        //How will we update repository info 
        _loadedRepositoryInfo = loadedRepositoryInfo;
        
        JsonDataManager.UserInfoChanged += OnUserInfoChanged;
    }

    private void OnUserInfoChanged(object? sender, EventArgs e)
    {
        GitInfoModel? userInfo = new();
        JsonDataManager.LoadUserInfo(ref userInfo);
        _loadedRepositoryInfo = userInfo?.SavedRepository;
    }

    /// <summary>
    /// Updates the commit history every tick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void UpdateCommitHistory(object? sender, EventArgs e)
    {
        //Check if commit needs to be updated or is same
        
        /*var commits = currentBranch.Commits.ToList();

        //Limits commit history to a certain length
        for (int i = 0; i < _commitHistoryLength; ++i)
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
    }*/

}