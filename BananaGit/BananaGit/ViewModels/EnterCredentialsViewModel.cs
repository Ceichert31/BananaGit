using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BananaGit.Models;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels
{
    partial class EnterCredentialsViewModel(EventHandler eventHandler) : ObservableObject
    {
        private readonly EventHandler onEnterCredentials = eventHandler;

        [ObservableProperty]
        private string _userToken = "";
        [ObservableProperty]
        private string _username = "";

        private readonly GithubUserInfo githubUserInfo = new();

        [RelayCommand]
        public void UpdateCredentials()
        {
            githubUserInfo.Username = Username;
            githubUserInfo.PersonalToken = UserToken;
            JsonDataManager.SaveUserInfo(githubUserInfo);
            onEnterCredentials?.Invoke(this, new EventArgs());
        }
    }
}
