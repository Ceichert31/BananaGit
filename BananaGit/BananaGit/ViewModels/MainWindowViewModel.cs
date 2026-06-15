using System.Diagnostics;
using System.IO;
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

        //Refactor for user controls!
        //Create an observable property for each view model here
        //Pass Git service to each vm during creation

        //Refactor for dialogs!
        //For dialog service pass git service through so
        //dialog's that use git commands can DI the service

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
                Trace.WriteLine("No locally saved user info.");

                //Create empty dialog and git service for log in window creation
                DialogService tempDialogService = new DialogService(new GitService(new GitInfoModel()));
                tempDialogService.ShowCredentialsDialog();
                return;
            }

            var gitService = new GitService(_userInfo);
            var dialogService = new DialogService(gitService);

            //Create view models
            ToolbarViewModel = new ToolbarViewModel(dialogService, gitService);
            CommitHistoryViewModel = new CommitHistoryViewModel(gitService);
            CommitViewModel = new CommitViewModel(gitService);
            GitChangesViewModel = new GitChangesViewModel(gitService, dialogService);

            gitService.OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}