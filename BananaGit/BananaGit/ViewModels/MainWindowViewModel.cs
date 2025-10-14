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

        private readonly GitInfoModel? userInfo;

        private readonly DialogService _dialogService = new();

        public MainWindowViewModel() 
        {
            //Load user info
            JsonDataManager.LoadUserInfo(ref userInfo);

            //If no user info could be loaded
            if (userInfo == null)
            {
                /* EnterCredentialsView = new EnterCredentialsView()
                 {
                     //Owner = App.Current.MainWindow
                 };
                 EnterCredentialsView.ShowDialog();*/

                _dialogService.ShowCredentialsDialog(_gitInfoViewModel);
            }

         /*   openCloneWindow += OpenCloneWindow;*/

            //_gitInfoViewModel = new();
        }
      /*  private void OpenCloneWindow(object? sender, EventArgs e)
        {
            CloneRepoView = new()
            {
                DataContext = _gitInfoViewModel,
                Owner = App.Current.MainWindow
            };
            CloneRepoView?.ShowDialog();
        }*/
    }
}
