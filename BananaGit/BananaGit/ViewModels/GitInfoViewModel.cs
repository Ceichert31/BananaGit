using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private string _usernameInput;
        [ObservableProperty]
        private string _repoName;
        [ObservableProperty]
        private string _commitMessage;

        [ObservableProperty]
        private string _repoInfo;


        //Clone properties
        [ObservableProperty]
        private string _cloneLocation;
        [ObservableProperty]
        private string _repoURL;


        //Flags
        [ObservableProperty]
        private bool _canClone;
        #endregion


        //private GitHubClient client;


        public GitInfoViewModel() 
        {
            /*client = new GitHubClient(new ProductHeaderValue("BananaGit"));

            //If we have credentials store, use them
            if (JsonDataManager.HasPersonalToken)
            {
                string credentials = JsonDataManager.GetGithubToken();
                client.Credentials = new Credentials(credentials);
            }*/
        }

        /*  [RelayCommand]
          public async Task UpdateCredentials()
          {
              var repo = await client.Repository.Get(UsernameInput, RepoName);

              RepoInfo = repo.PushedAt.ToString() ?? "No Commits";

              //Get latest commit from main
              var branchRef = await client.Git.Reference.Get(repo.Id, "heads/main");
              var lastCommit = await client.Git.Commit.Get(repo.Id, branchRef.Object.Sha);

              //Create new image to commit
              var imgBase64 = Convert.ToBase64String(File.ReadAllBytes(Environment.CurrentDirectory + "/Assets/Banana.png"));
              var blob = new NewBlob {  Encoding = EncodingType.Base64, Content = imgBase64 };
              var blobRef = await client.Git.Blob.Create(UsernameInput, RepoName, blob);

              //Add to new tree
              var tempTree = new NewTree { BaseTree = lastCommit.Tree.Sha };
              tempTree.Tree.Add(new NewTreeItem { Path = Environment.CurrentDirectory + "/Assets/Banana.png", Mode = "100644", Type = TreeType.Blob, Sha = blobRef.Sha});

              //Create new commit
              var newTree = await client.Git.Tree.Create(UsernameInput, RepoName, tempTree);

              var newCommit = new NewCommit(CommitMessage,  newTree.Sha, branchRef.Object.Sha);
              var commit = await client.Git.Commit.Create(UsernameInput, RepoName, newCommit);

              var headMasterRef = "heads/main";
              await client.Git.Reference.Update(UsernameInput, RepoName, headMasterRef, new ReferenceUpdate(commit.Sha));
          }*/

        [RelayCommand]
        public void CloneRepo()
        {
            CloneRepo(RepoURL, CloneLocation, UsernameInput, JsonDataManager.GetGithubToken());
        }

        [RelayCommand]
        public void ChooseCloneDirectory()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            if (dialog.ShowDialog() == true)
            {
                string selectedFilePath = dialog.FileName;

                //Check if directory is empty
                if (!Directory.EnumerateFiles(selectedFilePath).Any()) 
                {
                    CanClone = true;
                }
                else
                {
                    CanClone = false;
                }
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
        private static void CloneRepo(string repoURL, string repoPath, string username, string token)
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
