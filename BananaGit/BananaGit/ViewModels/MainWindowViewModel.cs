using System.Diagnostics;
using System.IO;
using System.Windows;
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

        private readonly UpdateService _updateService = new();

        private readonly DialogService _dialogService = new();


        private GitInfoModel? _userInfo;

        public MainWindowViewModel()
        {
            JsonDataManager.OnUserInfoChanged += Initialize;

            Initialize(this, EventArgs.Empty);

            _uiThread = SynchronizationContext.Current;
            _updateService.Initialize();
            Task.Run(CheckForUpdates);
#if RELEASE
#endif
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

        /// <summary>
        /// Service that checks for new updates
        /// </summary>
        private static readonly UpdateService UpdateService = new();

        private static SynchronizationContext? _uiThread;

        /// <summary>
        /// Checks for new updates and prompts user to update version
        /// </summary>
        private static async void CheckForUpdates()
        {
            try
            {
                // Check for new updates

                var hasUpdate = await UpdateService.CheckForUpdateAsync();

                if (!hasUpdate)
                    return;

                var info = UpdateService.GetUpdateInfo();

                if (info == null)
                    return;

                /*MessageBox.Show(
                    info.TargetFullRelease.NotesHTML,
                    "Update Checker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );*/

                /*_uiThread?.Post(
                    x =>
                    {
                        //ReleaseNotesDialog.Instance.OpenDialog(info.TargetFullRelease.NotesHTML);
                    },
                    null
                );*/

                var result = MessageBox.Show(
                    "Would you like to update?",
                    "Update Available",
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                    return;

                var canRestart = await UpdateService.DownloadLatest();

                if (!canRestart)
                    throw new TimeoutException("Couldn't download  the latest update!");

                var confirmRestart = MessageBox.Show(
                    "Restart to apply changes?",
                    "Restart Required",
                    MessageBoxButton.YesNo
                );

                if (confirmRestart != MessageBoxResult.Yes)
                    return;

                // Shutdown application when update is ready to be installed
                _uiThread?.Post(
                    x => { Application.Current.Shutdown(); },
                    null
                );
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}