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
        public string CanonicalName { get; set; }
        public bool IsRemote { get; set; }

        public GitBranch()
        {
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
        public GitBranch(GitInfoModel? gitInfo)
        {
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
            using var repo = new Repository(gitInfo.SavedRepository?.FilePath);
            
            var branch = repo.Branches[branchName];
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            CanonicalName = branch.CanonicalName;
        }

        /// <summary>
        /// Caches a branch into a model
        /// </summary>
        /// <param name="branch">The branch to cache</param>
        public GitBranch(Branch branch)
        {
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            CanonicalName = branch.CanonicalName;
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
                    using var repo = new Repository(gitInfo.GetPath());

                    var options = new FetchOptions();

                    options.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                    {
                        Username = gitInfo.Username,
                        Password = gitInfo.PersonalToken
                    };
                    
                    //Fetch latest
                    Commands.Fetch(repo, "origin", Array.Empty<string>(), options, "");
                    
                    var remoteBranch = repo.Branches[CanonicalName] ?? throw new NullReferenceException("No remote branch accessed from saved branch data");

                    string localName = Name.Replace("origin/", "");
                    
                    //Create a local tracking branch
                    Branch localTrackingBranch = repo.Branches.Add(localName, remoteBranch.Tip);
                    
                    //Update local branch
                    repo.Branches.Update(localTrackingBranch, x => x.TrackedBranch = CanonicalName);
                    
                    //Checkout branch
                    Commands.Checkout(repo, localTrackingBranch);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
