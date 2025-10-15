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

        public GitBranch(string name, bool isRemote)
        {
            Name = name;
            IsRemote = isRemote;
        }

        public GitBranch(Branch branch) 
        {
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
        }

        [RelayCommand]
        public void CheckoutBranch()
        {
            GitInfoModel? gitInfo = new();
            JsonDataManager.LoadUserInfo(ref gitInfo);

            if (gitInfo != null)
            {
                using var repo = new Repository(gitInfo.SavedRepository?.FilePath);
                var branch = repo.Branches[Name];

                //If branch doesn't exist, create a new one
                if (branch == null)
                {
                    repo.CreateBranch(Name);
                    branch = repo.Branches[Name];
                }

                //Checkout branch so it is no longer remote
                Branch currentBranch = Commands.Checkout(repo, branch);
                IsRemote = false;
            }
        }
    }
}
