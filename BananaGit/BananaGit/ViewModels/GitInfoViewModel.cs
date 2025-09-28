using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Win32;

namespace BananaGit.ViewModels
{
    partial class GitInfoViewModel : ObservableObject
    {
        #region Properties
        [ObservableProperty]
        private string _usernameInput = string.Empty;
        [ObservableProperty]
        private string _repoName = string.Empty;
        [ObservableProperty]
        private string _commitMessage = string.Empty;

        [ObservableProperty]
        private string _repoInfo = string.Empty;


        //Clone properties
        [ObservableProperty]
        private string _localRepoFilePath = string.Empty;
        [ObservableProperty]
        private string _repoURL = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _currentChanges = new();


        //Flags
        [ObservableProperty]
        private bool _canClone = false;
        #endregion

        private DispatcherTimer _updateGitInfoTimer = new();

        public GitInfoViewModel() 
        {
          /*  _updateGitInfoTimer.Tick += UpdateRepoStatus;
            _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(1000);*/
            //_updateGitInfoTimer.Start();
        }

        /// <summary>
        /// Update the current unstaged changes
        /// </summary>
        [RelayCommand]
        public void UpdateRepoStatus()
        {
            try
            {
                if (LocalRepoFilePath == string.Empty) return;

                using var repo = new Repository(LocalRepoFilePath);
                var stats = repo.RetrieveStatus(new StatusOptions());
                var untracked = stats.Untracked;
                foreach (var file in untracked)
                {
                    CurrentChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        [RelayCommand]
        public void CloneRepo()
        {
            CloneRepo(RepoURL, LocalRepoFilePath, UsernameInput, JsonDataManager.GetGithubToken());
        }

        [RelayCommand]
        public void ChooseCloneDirectory()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Multiselect = false;

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            if (dialog.ShowDialog() == true)
            {
                string selectedFilePath = dialog.FolderName;

                CanClone = true;
                LocalRepoFilePath = dialog.FolderName;

                //Check if directory is empty
                /* if (!Directory.EnumerateFiles(selectedFilePath).Any()) 
                 {
                     CanClone = true;
                     LocalRepoFilePath = dialog.FolderName;
                 }
                 else
                 {
                     CanClone = false;
                 }*/
            }
        }

        /// <summary>
        /// Clone a repository
        /// </summary>
        /// <param name="repoURL">The repositories URL</param>
        /// <param name="repoPath">Where the repository should be cloned to</param>
        /// <param name="username">The username of the user</param>
        /// <param name="token">The personal access token</param>
        /// <returns></returns>
        private void CloneRepo(string repoURL, string repoPath, string username, string token)
        {
            try
            {
                var options = new CloneOptions
                {
                    FetchOptions =
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                            {
                                Username = username,
                                Password = token
                            }
                    }
                };
                Repository.Clone(repoURL, repoPath, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
