using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels
{
    partial class EnterCredentialsViewModel(EventHandler eventHandler) : ObservableObject
    {
        private EventHandler onEnterCredentials = eventHandler;

        [ObservableProperty]
        private string _userToken = "";

        [RelayCommand]
        public void UpdateCredentials()
        {
            JsonDataManager.SaveGithubToken(UserToken);
            onEnterCredentials?.Invoke(this, new EventArgs());
        }
    }
}
