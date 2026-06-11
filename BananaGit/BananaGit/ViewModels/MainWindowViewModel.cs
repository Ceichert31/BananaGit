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
            Initialize();
        }

        private void Initialize()
        {
            //Load user info
            try
            {
                JsonDataManager.LoadUserInfo(ref _userInfo);
            }
            catch (IOException)
            {
                Trace.WriteLine("No locally saved user info.");

                DialogService tempDialogService = new DialogService(new GitService(new GitInfoModel()));
                tempDialogService.ShowCredentialsDialog();

                return;
            }

            GitService gitService = new GitService(_userInfo);

            //Passed into DialogService for dialog creation
            DialogService dialogService = new DialogService(gitService);

            ToolbarViewModel = new ToolbarViewModel(dialogService, gitService);

            CommitHistoryViewModel = new CommitHistoryViewModel(gitService);

            CommitViewModel = new CommitViewModel(gitService);

            GitChangesViewModel = new GitChangesViewModel(gitService, dialogService);

            //UpdateBranches(CurrentBranch);
            gitService.OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}