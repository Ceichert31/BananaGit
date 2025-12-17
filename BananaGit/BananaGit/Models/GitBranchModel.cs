using System.Diagnostics;
using BananaGit.Exceptions;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;

namespace BananaGit.Models
{
    partial class GitBranch : ObservableObject
    {
        public string Name { get; set; }
        public bool IsRemote { get; set; }
        public Branch Branch { get; set; }

        public GitBranch()
        {
            GitInfoModel? gitInfo = new();
            JsonDataManager.LoadUserInfo(ref gitInfo);

            try
            {
                if (gitInfo != null)
                {
                    //Check if saved repository exists
                    //Check here if file path and url exist, throw error that gets caught by GitInfoViewModel
                    //If no repos are cloned, display clone repo text
                    if (gitInfo.SavedRepository == null)
                    {
                        //NoRepoCloned = true;
                        throw new InvalidRepoException("No saved repository after loading!");
                    }
                    
                    //Check if file path is still valid
                    if (!Repository.IsValid(gitInfo.SavedRepository?.FilePath))
                    {
                        throw new InvalidRepoException("Saved file path is an invalid repo");
                    }

                    //Update info
                    using var repo = new Repository(gitInfo.SavedRepository?.FilePath);
                    
                    var options = new FetchOptions();

                    options.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                    {
                        Username = gitInfo.Username,
                        Password = gitInfo.PersonalToken
                    };
                    
                    string? branchName = Lib2GitSharpExt.GetDefaultRepoName(gitInfo.SavedRepository?.URL, options);

                    if (branchName == null)
                    {
                        throw new InvalidRepoException("Couldn't find default branch name");
                    }
                    
                    Branch = repo.Branches[branchName];
                    Name = Branch.FriendlyName;
                    IsRemote = Branch.IsRemote;
                }
                else
                {
                    throw new NullReferenceException("Couldn't load user info");
                }
            }
            catch (InvalidRepoException ex)
            {
                //If file path isn't a valid repo, clear file path

                /*f (gitInfo?.SavedRepository != null)
                {
                    gitInfo.SavedRepository.FilePath = string.Empty;
                }
                else
                {
                    if (gitInfo != null)
                        gitInfo.SavedRepository = new(string.Empty,string.Empty);
                }*/
                
                //JsonDataManager.SaveUserInfo(gitInfo);
                throw new GitException("No git repository saved or cloned");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
        public GitBranch(Branch branch)
        {
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            Branch = branch;
        }

        [RelayCommand]
        public void RemoveRemoteBranch()
        {
            if (!IsRemote) return;

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
            }
        }

        [RelayCommand]
        public void RemoveLocalBranch()
        {
            if (IsRemote) return;

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
            }
        }

        [RelayCommand]
        public void CheckoutBranch()
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
