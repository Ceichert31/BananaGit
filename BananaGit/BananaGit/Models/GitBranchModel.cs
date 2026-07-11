using System.IO;
using BananaGit.Exceptions;
using BananaGit.Services;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;

namespace BananaGit.Models
{
    public partial class GitBranch : ObservableObject
    {
        public string Name { get; set; }
        public string CanonicalName { get; set; }
        public bool IsRemote { get; set; }

        private readonly GitService _gitService;

        public GitBranch(GitService gitService)
        {
            _gitService = gitService;
            Name = "";
            CanonicalName = "";
        }

        /// <summary>
        /// Default constructor creates branch from HEAD
        /// </summary>
        /// <exception cref="RepoLocationException"> The repository saved no longer exists at that location </exception>
        /// <exception cref="LoadDataException"> Thrown if the user info is missing or can't be loaded </exception>
        /// <exception cref="GitException"> 
        /// An overarching git exception, if thrown something 
        /// relating to git operations or repositories has gone wrong 
        /// </exception>
        public GitBranch(GitInfoModel? gitInfo, GitService gitService)
        {
            _gitService = gitService;

            //Check user info has loaded
            if (gitInfo == null)
            {
                throw new LoadDataException("Couldn't load user info");
            }

            //Check if saved repository exists
            if (gitInfo.SavedRepository == null)
            {
                throw new InvalidRepoException("No saved repository after loading!");
            }

            //Check if repo location exists
            if (!Directory.Exists(gitInfo.SavedRepository?.FilePath))
            {
                throw new RepoLocationException("Local repository file location missing!");
            }

            //Check if file path is still valid
            if (!Repository.IsValid(gitInfo.SavedRepository?.FilePath))
            {
                throw new InvalidRepoException("Saved file path is an invalid repo");
            }

            //Get the name of the HEAD branch
            string? branchName = Lib2GitSharpExt.GetDefaultRepoName(gitInfo.GetUrl());

            if (branchName == null)
            {
                throw new InvalidRepoException("Couldn't find default branch name");
            }

            //Update branch info
            using var repo = new Repository(gitInfo.SavedRepository?.FilePath);

            var branch = repo.Branches[branchName];

            Commands.Checkout(repo, branch);

            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            CanonicalName = branch.CanonicalName;
        }

        /// <summary>
        /// Caches a branch into a model
        /// </summary>
        /// <param name="branch">The branch to cache</param>
        public GitBranch(Branch branch, GitService gitService)
        {
            _gitService = gitService;
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            CanonicalName = branch.CanonicalName;
        }

        [RelayCommand]
        private void CheckoutBranch()
        {
        }
    }
}