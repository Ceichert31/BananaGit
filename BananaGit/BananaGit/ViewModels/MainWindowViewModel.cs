using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels
{
    partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private GitInfoViewModel? _gitInfoViewModel;
        
        [ObservableProperty]
        private ToolbarViewModel? _toolbarViewModel;
        
        [ObservableProperty]
        private CommitHistoryViewModel? _commitHistoryViewModel;
        
        [ObservableProperty]
        private CommitViewModel? _commitViewModel;

        //Refactor for user controls!
        //Create an observable property for each view model here
        //Pass Git service to each vm during creation

        //Refactor for dialogs!
        //For dialog service pass git service through so
        //dialog's that use git commands can DI the service

        private readonly GitInfoModel? _userInfo;

        public MainWindowViewModel() 
        {
            //Load user info
            JsonDataManager.LoadUserInfo(ref _userInfo);
            
            GitService gitService = new GitService(_userInfo);
            
            //If no user info is loaded, display login dialog
            if (_userInfo == null)
            {
                DialogService tempDialogService = new DialogService(_gitInfoViewModel, gitService, new());
                tempDialogService.ShowCredentialsDialog();
                return;
            }
            
            //Passed into DialogService for dialog creation
            DialogService dialogService = new DialogService(_gitInfoViewModel, gitService, _userInfo);
            
            ToolbarViewModel = new ToolbarViewModel(dialogService, gitService, _userInfo);

            CommitHistoryViewModel = new CommitHistoryViewModel(gitService);

            CommitViewModel = new CommitViewModel(gitService);
            
            GitInfoViewModel = new GitInfoViewModel(gitService, _userInfo);
        }
    }
}
