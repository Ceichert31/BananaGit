using System.Windows.Threading;
using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels
{
    /// <summary>
    /// This view model is used to get user credentials and then save them with the <see cref="JsonDataManager"/>
    /// </summary>
    /// <param name="eventHandler"></param>
    /// <br/><br/>
    partial class LoginViewModel(EventHandler eventHandler) : ObservableObject
    {
        private readonly EventHandler onEnterCredentials = eventHandler;

        [ObservableProperty] private string _userToken = "";
        [ObservableProperty] private string _email = "";
        [ObservableProperty] private string _username = "";

        private readonly GitInfoModel _githubUserInfo = new();
        private readonly GithubAuthService _githubAuthService = new();

        /// <summary>
        /// Redirects user to a GitHub Authentication page and
        /// updates the front-end depending on a successful login or not
        /// </summary>
        [RelayCommand]
        private async Task UpdateCredentials()
        {
            //Run login async command

            //Get access token in return

            //Pass access token to GitInfoModel

            var githubAccessToken = await _githubAuthService.LoginAsync((userCode, url) =>
            {
                //Update frontend on completion
            });

            //Successful login
            if (!string.IsNullOrEmpty(githubAccessToken))
            {
                _githubUserInfo.Username = githubAccessToken;
                _githubUserInfo.PersonalToken = githubAccessToken;
                _githubUserInfo.Email = Email;
                JsonDataManager.SaveUserInfo(_githubUserInfo);
                onEnterCredentials?.Invoke(this, new CredentialsEventArgs(true));
            }
            //Unsuccessful login
            else
            {
                onEnterCredentials?.Invoke(this, new CredentialsEventArgs(false));
            }

            //Deprecated
            /*githubUserInfo.Username = Username;
            githubUserInfo.Email = Email;
            githubUserInfo.PersonalToken = UserToken;
            JsonDataManager.SaveUserInfo(githubUserInfo);
            onEnterCredentials?.Invoke(this, new EventArgs());*/
        }
    }
}