using System.Diagnostics;
using System.IO;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels
{
    partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty] private ToolbarViewModel? _toolbarViewModel;

        [ObservableProperty] private CommitHistoryViewModel? _commitHistoryViewModel;

        [ObservableProperty] private CommitViewModel? _commitViewModel;

        [ObservableProperty] private GitChangesViewModel? _gitChangesViewModel;

        private readonly DialogService _dialogService = new();


        private GitInfoModel? _userInfo;

        public MainWindowViewModel()
        {
            JsonDataManager.OnUserInfoChanged += Initialize;

            Initialize(this, EventArgs.Empty);
        }

        /// <summary>
        /// Loads user data and creates all required view models and models needed to perform git operations.
        /// If no user data is loaded, it opens up the log in window instead.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Initialize(object? sender, EventArgs e)
        {
            //Load user info
            try
            {
                JsonDataManager.LoadUserInfo(ref _userInfo);
            }
            catch (IOException)
            {
                GitService.OutputToConsole(this, new("No locally saved user info. Please sign in."));

                //Create empty dialog and git service for log in window creation
                _dialogService.ShowCredentialsDialog();
                return;
            }

            GitService? gitService = null;
            GitDialogService? gitDialogService = null;

            try
            {
                gitService = new GitService(_userInfo);
                gitDialogService = new GitDialogService(gitService);
            }
            catch (RepoLocationException)
            {
                GitService.OutputToConsole(this, new("No repository cloned"));
            }

            if (gitService == null || gitDialogService == null)
            {
                GitService.OutputToConsole(this, new("Something went wrong while initializing BananaGit!"));
                return;
            }


            //Create view models
            ToolbarViewModel = new ToolbarViewModel(gitDialogService, gitService);
            CommitHistoryViewModel = new CommitHistoryViewModel(gitService);
            CommitViewModel = new CommitViewModel(gitService);
            GitChangesViewModel = new GitChangesViewModel(gitService, gitDialogService);

            gitService.OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}