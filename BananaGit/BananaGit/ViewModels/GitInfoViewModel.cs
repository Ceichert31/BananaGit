using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Octokit;

namespace BananaGit.ViewModels
{
    partial class GitInfoViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _usernameInput;
        [ObservableProperty]
        private string _repoName;
        [ObservableProperty]
        private string _commitMessage;

        [ObservableProperty]
        private string _repoInfo;

        private GitHubClient client;

        private const string GITHUB_TOKEN = "";

        public GitInfoViewModel() 
        {
            client = new GitHubClient(new ProductHeaderValue("BananaGit"));
            client.Credentials = new Credentials(GITHUB_TOKEN);
        }

        [RelayCommand]
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
        }
    }
}
