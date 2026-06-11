using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Octokit;
using Application = System.Windows.Application;

namespace BananaGit.ViewModels
{
    /// <summary>
    /// This view model is used to get user credentials and then save them with the <see cref="JsonDataManager"/>
    /// </summary>
    /// <br/><br/>
    partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty] private string _displayText = "Pressing Sign in will redirect to browser.";
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
            string? githubAccessToken = null;

            //Attempt to access users GitHub account
            try
            {
                //Wait for login method to return a value
                githubAccessToken = await _githubAuthService.LoginAsync((userCode, url) =>
                {
                    //Update frontend on completion
                    Application.Current.Dispatcher.Invoke(() =>
                        {
                            DisplayText = "Please enter the code below in your browser window.";
                            UserCode = userCode;
                            Clipboard.SetText(UserCode);
                        }
                    );
                });
            }
            catch (AuthorizationException)
            {
                DisplayText = "Authorization failed. Please try again.";
                LoginFailed();
                return;
            }
            catch (TaskCanceledException)
            {
                DisplayText = "Login timed out. Please try again.";
                LoginFailed();
                return;
            }
            catch (HttpRequestException)
            {
                DisplayText = "Network connection failed. Check your connect and try again.";
                LoginFailed();
                return;
            }

            //Unsuccessful login
            if (string.IsNullOrEmpty(githubAccessToken))
            {
                DisplayText = "Login failed or timed out. Please try again.";
                LoginFailed();
                return;
            }

            //Successful login
            var userEmail = _githubAuthService.GetUserEmail();

            if (string.IsNullOrEmpty(userEmail))
            {
                DisplayText = "Logged in, but associated GitHub email couldn't be accessed.";
                LoginFailed();
                return;
            }

            //Update user info locally
            _githubUserInfo.Username = githubAccessToken;
            _githubUserInfo.PersonalToken = githubAccessToken;
            _githubUserInfo.Email = userEmail;

            try
            {
                //Save updated data
                JsonDataManager.SaveUserInfo(_githubUserInfo);
            }
            catch (IOException ex)
            {
                DisplayText = "Logged in, but credentials failed to save locally.";
                Trace.WriteLine(ex.Message);
                return;
            }

            _onEnterCredentials?.Invoke(this, new CredentialsEventArgs(true));
            DisplayText = "Successfully logged in!";
        }

        /// <summary>
        /// Copies the user code to the clipboard
        /// </summary>
        [RelayCommand]
        private void CopyUserCode()
        {
            Clipboard.SetText(UserCode);
            DisplayText = "Successfully copied code to clipboard.";
        }

        /// <summary>
        /// Clears user code and invokes a login failed event
        /// </summary>
        private void LoginFailed()
        {
            UserCode = string.Empty;
            _onEnterCredentials?.Invoke(this, new CredentialsEventArgs(false));
        }
    }
}