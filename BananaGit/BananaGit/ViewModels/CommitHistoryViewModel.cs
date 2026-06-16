using System.Collections.ObjectModel;
using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

/// <summary>
/// View model for the commit history view
/// </summary>
partial class CommitHistoryViewModel : ObservableObject
{
    [ObservableProperty] private string _repositoryName = string.Empty;

    //We will need to showcase the list inside of CommitHistoryPage

    [ObservableProperty] private ObservableCollection<CommitHistoryPage> _commitHistoryPages = [];

    /// <summary>
    /// The current commit history page information
    /// </summary>
    public ObservableCollection<GitCommitInfo>? CommitHistoryList => CommitHistoryPages.Count > 0
        ? CommitHistoryPages[(int)_pageIndex].CommitHistoryList
        : [];

    public bool IsLocalRepositoryOpen => !_gitService.IsLocalRepositoryOpen();

    private EventHandler<PageNumberEventArgs>? _onPageChanged;

    private uint _pageIndex = 0;

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

        CommitHistoryPages.Add(new CommitHistoryPage(gitService, HistoryLengthPerPage, 0, ref _onPageChanged));
        NotifyPageChanged();
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
            //Load the first page of history
            CommitHistoryPages.Clear();
            var pageOne = new CommitHistoryPage(_gitService, HistoryLengthPerPage, 0, ref _onPageChanged);
            CommitHistoryPages.Add(pageOne);

            NotifyPageChanged();

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
            //Load the first page of history
            CommitHistoryPages.Clear();
            var updatedPage = new CommitHistoryPage(_gitService, HistoryLengthPerPage, _pageIndex, ref _onPageChanged);
            CommitHistoryPages.Add(updatedPage);
            NotifyPageChanged();
        }
        catch (Exception ex)
        {
            GitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }

    [RelayCommand]
    private void GoForward()
    {
        _pageIndex++;

        CommitHistoryPages.Add(new CommitHistoryPage(_gitService, HistoryLengthPerPage, _pageIndex,
            ref _onPageChanged));
        NotifyPageChanged();
    }

    [RelayCommand]
    private void GoBackward()
    {
        if (_pageIndex <= 0)
            return;

        _pageIndex--;

        CommitHistoryPages.Add(new CommitHistoryPage(_gitService, HistoryLengthPerPage, _pageIndex,
            ref _onPageChanged));
        NotifyPageChanged();
    }

    private void NotifyPageChanged()
    {
        OnPropertyChanged(nameof(CommitHistoryList));
        _onPageChanged?.Invoke(this, new PageNumberEventArgs(_pageIndex));
    }
}