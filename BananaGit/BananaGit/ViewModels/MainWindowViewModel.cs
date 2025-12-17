using BananaGit.Models;
using BananaGit.Utilities;
using BananaGit.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels
{
    partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private GitInfoViewModel? _gitInfoViewModel;

        private readonly GitInfoModel? _userInfo;

        public MainWindowViewModel() 
        {
            //Load user info
            JsonDataManager.LoadUserInfo(ref _userInfo);
            
            _gitInfoViewModel = new();
            DialogService? dialogService = new(_gitInfoViewModel);

            //If no user info is loaded, display login dialog
            if (_userInfo == null)
            {
                dialogService.ShowCredentialsDialog();
            }
        }
    }
}
