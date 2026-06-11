using System.Windows;
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
    partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty] private string _userToken = "";
        [ObservableProperty] private string _email = "";
        [ObservableProperty] private string _username = "";
        [ObservableProperty] private string _displayText = "Hitting Confirm will redirect to browser.";
        [ObservableProperty] private string _userCode = "";

        private readonly GitInfoModel _githubUserInfo = new();
        private readonly GithubAuthService _githubAuthService = new();
        private readonly EventHandler<CredentialsEventArgs>? _onEnterCredentials;

        public LoginViewModel(EventHandler<CredentialsEventArgs> eventHandler)
        {
            _onEnterCredentials = eventHandler;
        }

        /// <summary>
        /// Redirects user to a GitHub Authentication page and
        /// updates the front-end depending on a successful login or not
        /// </summary>
        [RelayCommand]
        private async Task UpdateCredentials()
        {
            //Wait for login method to return a value
            var githubAccessToken = await _githubAuthService.LoginAsync((userCode, url) =>
            {
                //Update frontend on completion
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        DisplayText = "Please enter the code below in your browser window.";
                        UserCode = userCode;
                    }
                );
            });

            //Successful login
            if (!string.IsNullOrEmpty(githubAccessToken))
            {
                DisplayText = "Successfully logged in!";

                _githubUserInfo.Username = githubAccessToken;
                _githubUserInfo.PersonalToken = githubAccessToken;
                _githubUserInfo.Email = Email;
                JsonDataManager.SaveUserInfo(_githubUserInfo);
                _onEnterCredentials?.Invoke(this, new CredentialsEventArgs(true));
            }
            //Unsuccessful login
            else
            {
                DisplayText = "Login failed or timed out. Please try again.";
                _onEnterCredentials?.Invoke(this, new CredentialsEventArgs(false));
            }
        }

        /// <summary>
        /// Copies the user code to the clipboard
        /// </summary>
        [RelayCommand]
        private void CopyUserCode()
        {
            Clipboard.SetText(UserCode);
        }
    }
}