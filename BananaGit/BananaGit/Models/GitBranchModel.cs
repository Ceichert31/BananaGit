using System.Text.Json.Serialization;
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

        [JsonIgnore] private GitService _gitService;

        /// <summary>
        /// Empty constructor used to serialize JSON
        /// </summary>
        /// <remarks>
        /// If you need to initialize a branch, use <see cref="GitService"/>
        /// </remarks>
        public GitBranch()
        {
            _gitService = new GitService(null);
            Name = "";
            CanonicalName = "";
        }

        /// <summary>
        /// Attaches the <see cref="GitService"/> to the branch after it is loaded from file
        /// </summary>
        /// <param name="gitService"></param>
        public void AttachService(GitService gitService) => _gitService = gitService;

        /// <summary>
        /// Caches a branch into a model
        /// </summary>
        /// <param name="branch">The branch to cache</param>
        /// <param name="gitService">The Git Service that handles Git operations</param>
        public GitBranch(Branch branch, GitService gitService)
        {
            _gitService = gitService;
            Name = branch.FriendlyName;
            IsRemote = branch.IsRemote;
            CanonicalName = branch.CanonicalName;
        }

        /// <summary>
        /// Checks out a remote branch through the <see cref="GitService"/>
        /// </summary>
        [RelayCommand]
        private async Task CheckoutBranch()
        {
            try
            {
                await _gitService.CheckoutRemoteBranch(this);
            }
            catch (InvalidRepoException ex)
            {
                GitService.OutputToConsole(this, new(ex.Message));
            }
        }

        /// <summary>
        /// Deletes this branch locally through the <see cref="GitService"/>
        /// </summary>
        [RelayCommand]
        private async Task DeleteLocalBranch()
        {
            try
            {
                await _gitService.DeleteLocalBranch(Name.GetName());
                await _gitService.PullChanges();
            }
            catch (InvalidRepoException ex)
            {
                GitService.OutputToConsole(this, new(ex.Message));
            }
        }

        /// <summary>
        /// Deletes this branch on the remote through the <see cref="GitService"/>
        /// </summary>
        [RelayCommand]
        private async Task DeleteRemoteBranch()
        {
            try
            {
                await _gitService.DeleteRemoteBranch(Name.GetName());
                await _gitService.PullChanges();
            }
            catch (InvalidRepoException ex)
            {
                GitService.OutputToConsole(this, new(ex.Message));
            }
        }
    }
}