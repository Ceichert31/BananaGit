using System;
using System.Collections.Generic;
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
        private string _repoInfo;

        private GitHubClient client;

        public GitInfoViewModel() 
        {
            client = new GitHubClient(new ProductHeaderValue("BananaGit"));
        }

        [RelayCommand]
        public async Task UpdateCredentials()
        {
            var repo = await client.Repository.Get(UsernameInput, RepoName);
            RepoInfo = repo.CloneUrl;
        }
    }
}
