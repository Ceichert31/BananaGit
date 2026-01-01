using System.Diagnostics;
using System.IO;
using BananaGit.Exceptions;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;

namespace BananaGit.Models
{
    public partial class GitBranch : ObservableObject
    {
        public string Name { get; set; }
        public bool IsRemote { get; set; }
        public Branch Branch { get; set; }

        /// <summary>
        /// Default constructor creates branch from HEAD
        /// </summary>
        /// <exception cref="RepoLocationException"> The repository saved no longer exists at that location </exception>
        /// <exception cref="LoadDataException"> Thrown if the user info is missing or can't be loaded </exception>
        /// <exception cref="GitException"> 
        /// An overarching git exception, if thrown something 
        /// relating to git operations or repositories has gone wrong 
        /// </exception>
        public GitBranch()
        {
            GitInfoModel? gitInfo = new();
            JsonDataManager.LoadUserInfo(ref gitInfo);
            
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

            //Setup credentials for accessing remote branch info
            var options = new FetchOptions();
            options.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
            {
                Username = gitInfo.Username,
                Password = gitInfo.PersonalToken
            };

            //Get the name of the HEAD branch
            string? branchName = Lib2GitSharpExt.GetDefaultRepoName(gitInfo.GetUrl());

            if (branchName == null)
            {
                throw new InvalidRepoException("Couldn't find default branch name");
            }
            
            //Update branch info
            using (var repo = new Repository(gitInfo.SavedRepository?.FilePath))
            {
                //Fetch and pull remote
                //Commands.Fetch(repo, "origin", ["+refs/heads/*:refs/remotes/origin/*"], options, null);
                
                //If branch is a remote we need to make it a local branch here
                Branch = repo.Branches[branchName];
                Name = Branch.FriendlyName;
                IsRemote = Branch.IsRemote;
            }
        }

        /// <summary>
        /// Caches a branch into a model
        /// </summary>
        /// <param name="branch">The branch to cache</param>
        public GitBranch(Branch branch)
        {
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            Branch = branch;
        }

        [RelayCommand]
        public void RemoveRemoteBranch()
        {
            /*if (!IsRemote) return;

            try
            {
                GitInfoModel? gitInfo = new();
                JsonDataManager.LoadUserInfo(ref gitInfo);

                if (gitInfo != null)
                {
                    using var repo = new Repository(gitInfo.SavedRepository?.FilePath);

                    repo.Network.Remotes.Remove(Branch.RemoteName);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }*/
        }

        [RelayCommand]
        private void RemoveLocalBranch()
        {
            /*if (IsRemote) return;

            try
            {
                GitInfoModel? gitInfo = new();
                JsonDataManager.LoadUserInfo(ref gitInfo);

                if (gitInfo != null)
                {
                    using var repo = new Repository(gitInfo.SavedRepository?.FilePath);

                    repo.Branches.Remove(Branch.CanonicalName);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }*/
        }

        [RelayCommand]
        private void CheckoutBranch()
        {
            if (!IsRemote) return;

            try
            {
                GitInfoModel? gitInfo = new();
                JsonDataManager.LoadUserInfo(ref gitInfo);

                if (gitInfo != null)
                {
                    using var repo = new Repository(gitInfo.SavedRepository?.FilePath);

                    var options = new FetchOptions();

                    options.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                    {
                        Username = gitInfo.Username,
                        Password = gitInfo.PersonalToken
                    };

                    //Fetch remotes
                    Commands.Fetch(repo, Branch.RemoteName, new string[0], options, null);

                    string localBranchName = Branch.FriendlyName.Remove(0, 7);
                    Branch localBranch = repo.CreateBranch(localBranchName, Branch.Tip);
                    repo.Branches.Update(localBranch, b => b.TrackedBranch = Branch.CanonicalName);
                    Commands.Checkout(repo, localBranch);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
