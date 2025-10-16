using System.Windows.Controls;
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

            if (gitInfo != null)
            {
                using var repo = new Repository(gitInfo.SavedRepository?.FilePath);
                Branch = repo.Branches["main"];
                Name = Branch.FriendlyName;
                IsRemote = Branch.IsRemote;
            }
            else
            {
                throw new NullReferenceException("Couldn't load user info");
            }
        }
        public GitBranch(Branch branch)
        {
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            Branch = branch;
        }

        [RelayCommand]
        public void CheckoutBranch()
        {
            GitInfoModel? gitInfo = new();
            JsonDataManager.LoadUserInfo(ref gitInfo);

            if (gitInfo != null)
            {
                using var repo = new Repository(gitInfo.SavedRepository?.FilePath);

                //Fetch remotes

                var options = new FetchOptions();

                options.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                {
                    Username = gitInfo.Username,
                    Password = gitInfo.PersonalToken
                };
                Commands.Fetch(repo, Branch.RemoteName, new string[0], options, null);

                string localBranchName = Branch.FriendlyName.Remove(0, 7);
                Branch localBranch = repo.CreateBranch(localBranchName, Branch.Tip);
                repo.Branches.Update(localBranch, b => b.TrackedBranch = Branch.CanonicalName);
                Commands.Checkout(repo, localBranch);
            }


        }
    }
}
