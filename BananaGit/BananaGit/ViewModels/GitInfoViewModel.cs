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

        private SaveableRepository currentRepo;

        //Flags
        [ObservableProperty]
        private bool _canClone = false;

        private bool hasCloned = false;
        #endregion

        private readonly DispatcherTimer _updateGitInfoTimer = new();

        private GithubUserInfo? githubUserInfo;

        public GitInfoViewModel() 
        {
            _updateGitInfoTimer.Tick += UpdateRepoStatus;
            _updateGitInfoTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _updateGitInfoTimer.Start();

            JsonDataManager.LoadUserInfo(ref githubUserInfo);

            if (githubUserInfo == null)
            {
                throw new Exception("ERROR: Data couldn't be loaded!");
            }

            if (githubUserInfo.SavedRepositories?[0] != null)
            {
                currentRepo = githubUserInfo.SavedRepositories[0];
                LocalRepoFilePath = currentRepo.FilePath;
                RepoURL = currentRepo.URL;
                hasCloned = true;
            }
              
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
                githubUserInfo.SavedRepositories[0].URL = RepoURL;
                JsonDataManager.SaveUserInfo(githubUserInfo);
            }
            else if (e.PropertyName == nameof(LocalRepoFilePath))
            {
                githubUserInfo.SavedRepositories[0].FilePath = LocalRepoFilePath;
                JsonDataManager.SaveUserInfo(githubUserInfo);
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
                if (!hasCloned) return;
                if (githubUserInfo.SavedRepositories == null) return;
                if (githubUserInfo.SavedRepositories[0] == null) return;

                var localRepo = githubUserInfo.SavedRepositories[0];

                if (localRepo.FilePath == null || localRepo.FilePath == "") return;

                CurrentChanges.Clear();
                StagedChanges.Clear();

                using var repo = new Repository(localRepo.FilePath);
                var stats = repo.RetrieveStatus(new StatusOptions());

                //If no changes have been made, don't do anything
                if (!stats.IsDirty) return;

                var untracked = stats.Untracked;
                var changed = stats.Modified;
                var staged = stats.Staged;
                var added = stats.Added;
                foreach (var file in untracked)
                {
                    CurrentChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
                foreach (var file in changed)
                {
                    CurrentChanges.Add($"+{file.FilePath}" ?? "N/A");
                }
                foreach (var file in staged)
                {
                    StagedChanges.Add($"+{file.FilePath}");
                }
                foreach (var file in added)
                {
                    StagedChanges.Add($"+{file.FilePath}" ?? "N/A");
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
            if (githubUserInfo == null) return;

            CloneRepo(githubUserInfo);
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
                    Signature author = new Signature(githubUserInfo?.Username, "ceichert3114@gmail.com", DateTime.Now);
                    Signature committer = author;

                    Commit commit = repo.Commit(CommitMessage, author, committer);
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to commit {LocalRepoFilePath}");
                throw;
            }
        }

        [RelayCommand]
        public void StageFiles()
        {
            try
            {
                var files = Directory.EnumerateFiles(currentRepo.FilePath);

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
                throw;
            }
        }

        [RelayCommand]
        public void PushFiles()
        {
            try
            {
                using (var repo = new Repository(currentRepo.FilePath))
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
                            Username = githubUserInfo?.Username,
                            Password = githubUserInfo?.PersonalToken
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
                            Username = githubUserInfo?.Username,
                            Password = githubUserInfo?.PersonalToken
                        }
                    };

                    repo.Network.Push(localBranch, pushOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to Push {ex.Message}");
                throw;
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

                //Save repo to github user info
                githubUserInfo?.SavedRepositories?.Add(new(LocalRepoFilePath, RepoURL));

                Repository.Clone(RepoURL, LocalRepoFilePath, options);
                hasCloned = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to Clone {ex.Message}");
                throw;
            }
            finally
            {
                JsonDataManager.SaveUserInfo(githubUserInfo);
            }
        }
    }
}
