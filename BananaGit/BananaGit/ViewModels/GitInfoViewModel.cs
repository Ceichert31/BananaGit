using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;
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


        //Branches
        [ObservableProperty]
        private string _branchName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _currentChanges = new();
        [ObservableProperty]
        private ObservableCollection<string> _stagedChanges = new();


        //Flags
        [ObservableProperty]
        private bool _canClone = false;
        #endregion

        private DispatcherTimer _updateGitInfoTimer = new();

        public GitInfoViewModel() 
        {
            _updateGitInfoTimer.Tick += UpdateRepoStatus;
            _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _updateGitInfoTimer.Start();

            PropertyChanged += UpdateCurrentRepository;
        }

        private void UpdateRepoStatus(object? sender, EventArgs e)
        {
            UpdateRepoStatus();
        }

        private void UpdateCurrentRepository(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RepoURL))
            {
                JsonDataManager.SetCurrentRepoURL(RepoURL);
            }
            else if (e.PropertyName == nameof(LocalRepoFilePath))
            {
                JsonDataManager.SetCurrentRepoFilePath(LocalRepoFilePath);
            }
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

                CurrentChanges.Clear();

                using var repo = new Repository(LocalRepoFilePath);
                var stats = repo.RetrieveStatus(new StatusOptions());
                var untracked = stats.Untracked;
                var added = stats.Added;
                foreach (var file in untracked)
                {
                    CurrentChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
                foreach (var file in added)
                {
                    StagedChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        public void CloneRepo()
        {
            if (JsonDataManager.UserInfo == null) return;

            CloneRepo(JsonDataManager.UserInfo);
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

        [RelayCommand]
        public void CommitStagedFiles()
        {
            try
            {
                using (var repo = new Repository(LocalRepoFilePath))
                {
                    Signature author = new Signature(JsonDataManager.UserInfo.Username, "ceichert3114@gmail.com", DateTime.Now);
                    Signature committer = author;

                    Commit commit = repo.Commit("User input here", author, committer);
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to commit {LocalRepoFilePath}");
            }
        }

        [RelayCommand]
        public void StageFiles()
        {
            try
            {
                var files = Directory.EnumerateFiles(JsonDataManager.UserInfo.FilePath);

               using (var repo = new Repository(LocalRepoFilePath))
                {
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        repo.Index.Add(fileName);
                        repo.Index.Write();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to stage {ex.Message}");
            }
        }

        [RelayCommand]
        public void PushFiles()
        {
            try
            {
                using (var repo = new Repository(LocalRepoFilePath))
                {
                    var remote = repo.Network.Remotes["origin"];
                    if (remote != null)
                    {
                        repo.Network.Remotes.Remove("origin");
                    }

                    repo.Network.Remotes.Add("origin", RepoURL);
                    remote = repo.Network.Remotes["origin"];
                    if (remote == null) return;

                    FetchOptions options = new FetchOptions
                    {
                        CredentialsProvider = (url, username, types) => 
                        new UsernamePasswordCredentials
                        {
                            Username = JsonDataManager.UserInfo?.Username,
                            Password = JsonDataManager.UserInfo?.PersonalToken
                        }
                    };

                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, options, string.Empty);

                    var localBranchName = string.IsNullOrEmpty(BranchName) ? "main" : BranchName;
                    var localBranch = repo.Branches[localBranchName];

                    if (localBranch == null) return;

                    repo.Branches.Update(localBranch, 
                        b => b.Remote = remote.Name, 
                        b => b.UpstreamBranch = localBranch.CanonicalName);

                    var pushOptions = new PushOptions
                    {
                        CredentialsProvider = (url, username, types) => 
                        new UsernamePasswordCredentials
                        {
                            Username = JsonDataManager.UserInfo?.Username,
                            Password = JsonDataManager.UserInfo?.PersonalToken
                        }
                    };

                    repo.Network.Push(localBranch, pushOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to Push {ex.Message}");
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
        private void CloneRepo(GithubUserInfo userInfo)
        {
            try
            {
                var options = new CloneOptions
                {
                    FetchOptions =
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                            {
                                Username = userInfo.Username,
                                Password = userInfo.PersonalToken
                            }
                    }
                };
                Repository.Clone(userInfo.URL, userInfo.FilePath, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to Clone {ex.Message}");
            }
        }
    }
}
