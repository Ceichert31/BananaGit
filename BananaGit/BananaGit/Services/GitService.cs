using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using BananaGit.EventArgExtensions;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Utilities;
using LibGit2Sharp;
using Microsoft.Win32;

namespace BananaGit.Services
{
    /// <summary>
    /// Handles all git commands and operations
    /// </summary>
    public class GitService
    {
        private GitInfoModel? _gitInfo;

        public EventHandler<EventArgs>? OnRepositoryChanged;
        public EventHandler<EventArgs>? OnChangesPulled;


        /// <summary>
        /// The currently selected branch that Git operations will be executed on
        /// </summary>
        public GitBranch? CurrentBranch
        {
            get => _gitInfo?.CurrentBranch;
            set
            {
                //If no git info exists, create a new instance
                _gitInfo ??= new GitInfoModel();

                _gitInfo.CurrentBranch = value;
            }
        }

        public GitService(GitInfoModel? gitInfo)
        {
            _gitInfo = gitInfo;
            JsonDataManager.OnUserInfoChanged += OnUserDataChange;

            // Attach this service to the current branch after its been loaded
            _gitInfo?.CurrentBranch?.AttachService(this);
        }

        /// <summary>
        /// Updates the current user info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUserDataChange(object? sender, EventArgs e)
        {
            JsonDataManager.LoadUserInfo(ref _gitInfo);
            _gitInfo?.CurrentBranch?.AttachService(this);
        }

        /// <summary>
        /// Outputs the name of the sender of an exception and the exception message
        /// </summary>
        /// <param name="sender">The script sending this message</param>
        /// <param name="e">The message</param>
        public static void OutputToConsole(object? sender, MessageEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (sender == null)
                {
                    Trace.WriteLine($"Unknown origin: {e.Message}");
                    return;
                }

                Trace.WriteLine($"{sender.GetType().ToString().Split('.').Last()}: {e.Message}");
            });
        }

        #region Repository Status Methods

        /// <summary>
        /// Checks whether a local repository is currently open
        /// </summary>
        /// <returns></returns>
        public bool IsLocalRepositoryOpen()
        {
            return !string.IsNullOrEmpty(_gitInfo?.GetPath());
        }

        /// <summary>
        /// Checks if the current repo has files commited but not pushed
        /// </summary>
        /// <returns>Whether locally commited files have been pushed</returns>
        public bool HasLocalCommitedFiles()
        {
            using var repo = new Repository(_gitInfo?.GetPath());

            //Check if branch has a remote tracking branch
            if (repo.Head.TrackedBranch == null)
            {
                return false;
            }

            //Start at local head tip and query until remote head tip
            var localCommits = repo.Commits.QueryBy(
                new CommitFilter
                {
                    IncludeReachableFrom = repo.Head.Tip.Id,
                    ExcludeReachableFrom = repo.Head.TrackedBranch.Tip.Id,
                }
            );

            return localCommits.Any();
        }

        #endregion

        #region Repository Query Methods

        /// <summary>
        /// Checks if the repository is accessible, and gets the repository name
        /// </summary>
        /// <returns>The name of the current repository name</returns>
        /// <exception cref="NullReferenceException"></exception>
        public string GetRepositoryName()
        {
            if (_gitInfo?.TryGetPath(out var path) == null)
            {
                throw new NullReferenceException("Couldn't access repository path!");
            }

            VerifyPath();

            return new DirectoryInfo(path).Name;
        }

        /// <summary>
        /// Takes in a range of numbers and retrieves the commit history of that index
        /// </summary>
        /// <param name="min">The start of the list</param>
        /// <param name="max">The end of the list</param>
        /// <returns></returns>
        public List<GitCommitInfo> GetCommitHistoryRange(uint min, uint max)
        {
            VerifyPath();

            //Prevent min being greater than max
            ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);

            using var repo = new Repository(_gitInfo?.GetPath());

            if (_gitInfo?.CurrentBranch == null)
            {
                if (_gitInfo != null)
                {
                    _gitInfo.CurrentBranch = InitializeMainBranch();
                }
                else
                {
                    JsonDataManager.LoadUserInfo(ref _gitInfo);
                }
            }

            var commits = repo.Branches[_gitInfo?.CurrentBranch?.CanonicalName].Commits.ToList();

            List<GitCommitInfo> commitList = new();

            //Clamp
            if (max > commits.Count)
            {
                max = (uint)commits.Count;
            }

            //Iterate through and convert commit info into GitCommitInfo model
            for (var i = 0; i < max; i++)
            {
                if (i < min)
                    continue;

                GitCommitInfo commitInfo = new()
                {
                    Author = commits[i].Author.ToString(),
                    Date =
                        $"{commits[i].Author.When.DateTime.ToShortTimeString()} {commits[i].Author.When.DateTime.ToShortDateString()}",
                    Message = commits[i].Message,
                    Commit = commits[i].Id.ToString(),
                    //Check if more than one parent, then it is a merge commit
                    IsMergeCommit = commits[i].Parents.Count() > 1
                };
                commitList.Add(commitInfo);
            }

            return commitList;
        }

        /// <summary>
        /// Checks if there have been any local changes
        /// </summary>
        /// <returns>Returns true if there has been local changes</returns>
        public bool HasLocalChanges()
        {
            VerifyPath();

            using var repo = new Repository(_gitInfo?.GetPath());

            var staged = repo.RetrieveStatus().Staged;
            var added = repo.RetrieveStatus().Added;
            var removed = repo.RetrieveStatus().Removed;

            //Check if any changes exist 
            return staged.Any() || added.Any() || removed.Any();
        }

        /// <summary>
        /// Returns a list of all unstaged local changes
        /// </summary>
        /// <returns>A list of all unstaged local changes</returns>
        public List<ChangedFile> GetUnstagedChanges()
        {
            VerifyPath();
            using var repo = new Repository(_gitInfo?.GetPath());

            var stats = repo.RetrieveStatus(new StatusOptions());

            List<ChangedFile> changedFiles = new();

            //Find all unstaged changed files and add them to list
            foreach (var file in stats)
            {
                if (file.State is FileStatus.ModifiedInWorkdir or FileStatus.NewInWorkdir ||
                    file.State == FileStatus.RenamedInWorkdir ||
                    file.State == FileStatus.DeletedFromWorkdir ||
                    file.State == (FileStatus.NewInIndex | FileStatus.ModifiedInWorkdir))
                {
                    changedFiles.Add(new ChangedFile(this, file, file.FilePath));
                }
            }

            return changedFiles;
        }

        /// <summary>
        /// Returns a list of all staged local changes
        /// </summary>
        /// <returns>A list of all staged local changes</returns>
        public List<ChangedFile> GetStagedChanges()
        {
            VerifyPath();
            using var repo = new Repository(_gitInfo?.GetPath());

            var stats = repo.RetrieveStatus(new StatusOptions());

            List<ChangedFile> changedFiles = new();

            //Find all unstaged changed files and add them to list
            foreach (var file in stats)
            {
                if (file.State == FileStatus.ModifiedInIndex ||
                    file.State == FileStatus.NewInIndex ||
                    file.State == FileStatus.RenamedInIndex ||
                    file.State == FileStatus.DeletedFromIndex)
                {
                    changedFiles.Add(new ChangedFile(this, file, file.FilePath));
                }
            }

            return changedFiles;
        }

        #endregion

        #region Branch Methods

        #region Branch Getters

        /// <summary>
        /// Verifies repository path and returns a list of local branches
        /// </summary>
        /// <returns>A list of local branches</returns>
        public List<GitBranch> GetLocalBranches()
        {
            VerifyPath();

            using var repo = new Repository(_gitInfo?.GetPath());

            List<GitBranch> localBranches = new();

            foreach (var branch in repo.Branches)
            {
                if (branch.IsRemote)
                {
                    continue;
                }

                localBranches.Add(new GitBranch(branch, this));
            }

            return localBranches;
        }

        /// <summary>
        /// Verifies repository path and returns a list of remote branches
        /// </summary>
        /// <returns>A list of remote branches</returns>
        public List<GitBranch> GetRemoteBranches()
        {
            VerifyPath();

            using var repo = new Repository(_gitInfo?.GetPath());

            List<GitBranch> remoteBranches = new();

            foreach (var branch in repo.Branches)
            {
                if (!branch.IsRemote)
                {
                    continue;
                }

                remoteBranches.Add(new GitBranch(branch, this));
            }

            return remoteBranches;
        }

        #endregion


        /// <summary>
        /// Initializes the default main branch
        /// </summary>
        /// <returns>The main branch as a GitBranch model</returns>
        /// <exception cref="InvalidRepoException">Thrown if default branch can't be found</exception>
        private GitBranch InitializeMainBranch()
        {
            //Get the name of the HEAD branch
            string? branchName = Lib2GitSharpExt.GetDefaultRepoName(_gitInfo?.GetUrl());

            if (branchName == null)
            {
                throw new InvalidRepoException("Couldn't find default branch name");
            }

            //Update branch info
            using var repo = new Repository(_gitInfo?.SavedRepository?.FilePath);

            var branch = repo.Branches[branchName];

            Commands.Checkout(repo, branch);

            return new GitBranch(branch, this);
        }

        /// <summary>
        /// Creates a new branch and pushes it to the remote repository
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="branchName"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task CreateBranchAsync(GitBranch origin, string branchName)
        {
            using var repo = new Repository(_gitInfo?.GetPath());

            Branch originBranch = repo.Branches[origin.Name] ??
                                  throw new InvalidOperationException($"Failed to find {origin.Name} branch");

            // Create new branch off of origin branch
            Branch newBranch = repo.CreateBranch(branchName, originBranch.Tip);

            // Set tracking
            if (originBranch.IsRemote)
            {
                newBranch = repo.Branches.Update(newBranch, b => b.TrackedBranch = originBranch.CanonicalName);
            }

            // Checkout the new branch right after creation
            Commands.Checkout(repo, newBranch);

            await PushBranchAsync(branchName);
        }

        /// <summary>
        /// Finds a branch by name and pushes it to the remote repository
        /// </summary>
        /// <param name="branchName">Name of the target branch</param>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task PushBranchAsync(string branchName)
        {
            // Push new local branch to the remote

            // Get credentials/push options

            await Task.Run(() =>
            {
                using var repo = new Repository(_gitInfo?.GetPath());

                var branch = repo.Branches[branchName] ??
                             throw new InvalidOperationException($"Failed to find {branchName} branch");

                Remote remote = repo.Network.Remotes["origin"];

                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (_, _, _) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _gitInfo?.PersonalToken,
                            Password = _gitInfo?.PersonalToken
                        }
                };

                repo.Branches.Update(branch, x => x.Remote = remote.Name, x => x.UpstreamBranch = branch.CanonicalName);

                repo.Network.Push(branch, pushOptions);
            });
        }

        /// <summary>
        /// Checks out a remote branch locally
        /// </summary>
        /// <param name="branch">The branch to check out </param>
        /// <exception cref="InvalidBranchException">Thrown if remote can't be found</exception>
        public async Task CheckoutRemoteBranch(GitBranch branch)
        {
            await Task.Run(() =>
            {
                if (!branch.IsRemote) return;

                using var repo = new Repository(_gitInfo?.GetPath());

                var options = new FetchOptions
                {
                    CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
                    {
                        Username = _gitInfo?.Username,
                        Password = _gitInfo?.PersonalToken
                    }
                };

                //Fetch latest
                Commands.Fetch(repo, "origin", Array.Empty<string>(), options, "");

                var remoteBranch = repo.Branches[branch.CanonicalName] ??
                                   throw new InvalidBranchException(
                                       $"Couldn't find branch: {branch.Name}");

                string localName = branch.Name.Replace("origin/", "");

                //Create a local tracking branch
                Branch localTrackingBranch = repo.Branches.Add(localName, remoteBranch.Tip);

                //Update local branch
                repo.Branches.Update(localTrackingBranch, x => x.TrackedBranch = branch.CanonicalName);

                //Checkout branch
                Commands.Checkout(repo, localTrackingBranch);
            });
        }

        /// <summary>
        /// Deletes a specified local branch, remote branch remains untouched
        /// </summary>
        /// <param name="branchName">The friendly name of the branch</param>
        public async Task DeleteLocalBranch(string branchName)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(_gitInfo?.GetPath());

                // Switch branches if we are on the branch we want to delete
                if (string.Equals(repo.Head.FriendlyName, branchName))
                {
                    var mainBranch = Lib2GitSharpExt.GetDefaultRepoName(_gitInfo?.GetUrl());

                    if (mainBranch == null)
                        throw new InvalidBranchException($"Failed to find default branch");

                    Commands.Checkout(repo, mainBranch);
                }

                // Local branch deletion
                if (repo.Branches[branchName] == null) return;

                repo.Branches.Remove(branchName);
                OutputToConsole(this, new($"Successfully deleted local branch: {branchName}"));
            });
        }

        /// <summary>
        /// Deletes a specified remote branch, cleans up any associated local branches
        /// </summary>
        /// <param name="branchName">The branch to delete</param>
        /// <exception cref="InvalidBranchException">Thrown if the default branch (ex. main) can't be found</exception>
        public async Task DeleteRemoteBranch(string branchName)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(_gitInfo?.GetPath());

                var mainBranch = Lib2GitSharpExt.GetDefaultRepoName(_gitInfo?.GetUrl());

                if (mainBranch == null)
                    throw new InvalidBranchException($"Failed to find default branch");

                var remote = repo.Network.Remotes["origin"];

                // Command to delete remote
                string pushRefSpec = $":refs/heads/{branchName.GetName()}";

                // Setup credentials to push changes
                PushOptions pushOptions = new()
                {
                    CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials()
                    {
                        Username = _gitInfo?.PersonalToken,
                        Password = _gitInfo?.PersonalToken,
                    }
                };

                // Switch branches if we are on the branch we want to delete
                if (string.Equals(repo.Head.FriendlyName, branchName))
                {
                    Commands.Checkout(repo, mainBranch);
                    CurrentBranch = InitializeMainBranch();
                }

                // Error being thrown because remote is null?
                repo.Network.Push(remote, pushRefSpec, pushOptions);
                OutputToConsole(this, new($"Successfully deleted remote branch: {branchName}"));

                // Cleanup local branch if it exists
                if (repo.Branches[$"origin/{branchName}"] == null) return;

                repo.Branches.Remove($"origin/{branchName}");
                OutputToConsole(this, new($"Successfully deleted local branch: {branchName}"));
            });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Removes all local commits and reverts to the remotes last commit
        /// (Aka deletes all unpushed commits)
        /// </summary>
        public async Task ResetLocalCommitsAsync()
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(_gitInfo?.GetPath());

                //Move HEAD to remotes last commit
                repo.Reset(ResetMode.Hard, repo.Head.TrackedBranch.Tip);
            });
        }

        /// <summary>
        /// Reset only local uncommited files
        /// </summary>
        public async Task ResetLocalUncommittedFilesAsync()
        {
            //May have to make custom command

            await Task.Run(() =>
            {
                using var repo = new Repository(_gitInfo?.GetPath());

                //Move HEAD to remotes last commit
                repo.Reset(ResetMode.Hard);
            });
        }

        /// <summary>
        /// Resets a specific file to the remote version
        /// </summary>
        /// <param name="filePath">The path to the file to reset</param>
        public async Task ResetLocalFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(_gitInfo?.GetPath());

                repo.CheckoutPaths("HEAD", [filePath], new CheckoutOptions
                {
                    CheckoutModifiers = CheckoutModifiers.Force
                });
            });
        }

        /// <summary>
        /// Checks current repositories file location before using it
        /// </summary>
        /// <exception cref="RepoLocationException"></exception>
        private void VerifyPath()
        {
            if (string.IsNullOrEmpty(_gitInfo?.GetPath()) || !Directory.Exists(_gitInfo?.GetPath()))
            {
                throw new RepoLocationException(
                    "Local repository file path is missing. Please clone or open a local repository before performing git operations.");
            }
        }

        /// <summary>
        /// Checks if the current branch is not null, if the current branch is null, attempt to find the default branch
        /// </summary>
        /// <returns>Whether the current branch is null</returns>
        [MemberNotNullWhen(true, nameof(CurrentBranch))]
        private bool VerifyCurrentBranch()
        {
            if (CurrentBranch != null) return true;

            try
            {
                CurrentBranch = InitializeMainBranch();
            }
            catch (LoadDataException ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
            catch (InvalidRepoException ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
            catch (RepoLocationException ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// The callback for conflicts
        /// </summary>
        /// <param name="path">The file that conflicted</param>
        /// <param name="notifyFlags">The checkout notify flag</param>
        /// <returns></returns>
        private bool ShowConflict(string path, CheckoutNotifyFlags notifyFlags)
        {
            if (notifyFlags == CheckoutNotifyFlags.Conflict)
            {
                Trace.WriteLine($"Conflict found in file {path}");
            }

            return true;
        }

        #endregion

        #region Stage/Commit

        /// <summary>
        /// Commits all staged files
        /// </summary>
        /// <param name="commitMessage">The message for this commit</param>
        public async Task CommitStagedFilesAsync(string commitMessage)
        {
            await Task.Run(() =>
            {
                VerifyPath();

                using var repo = new Repository(_gitInfo?.GetPath());

                //Set author for commiting
                Signature author = new(_gitInfo?.Username, _gitInfo?.Email, DateTime.Now);
                repo.Commit(commitMessage, author, author);
            });
        }

        /// <summary>
        /// Stages all changed files in the working directory
        /// </summary>
        public async Task StageFilesAsync()
        {
            await Task.Run(() =>
            {
                VerifyPath();

                using var repo = new Repository(_gitInfo?.GetPath());
                //Get status and return if no changes have been made
                var status = repo.RetrieveStatus();
                if (!status.IsDirty)
                    return;

                foreach (var file in status)
                {
                    if (file.State == FileStatus.Ignored)
                        continue;

                    Commands.Stage(repo, file.FilePath);
                }
            });
        }

        /// <summary>
        /// Unstages all files in the working directory
        /// </summary>
        public async Task UnstageFilesAsync()
        {
            await Task.Run(() =>
            {
                VerifyPath();

                using var repo = new Repository(_gitInfo?.GetPath());
                //Get status and return if no changes have been made
                var status = repo.RetrieveStatus();
                if (!status.IsDirty)
                    return;

                foreach (var file in status)
                {
                    if (file.State == FileStatus.Ignored)
                        continue;

                    Commands.Unstage(repo, file.FilePath);
                }
            });
        }

        /// <summary>
        /// Stages a specific file
        /// </summary>
        /// <param name="fileToStage">The file that is going to be staged</param>
        public async Task StageFileAsync(ChangedFile fileToStage)
        {
            await Task.Run(() =>
            {
                VerifyPath();

                using var repo = new Repository(_gitInfo?.SavedRepository?.FilePath);
                var status = repo.RetrieveStatus();
                if (!status.IsDirty)
                    return;

                Commands.Stage(repo, fileToStage.FilePath);
            });
        }

        /// <summary>
        /// Stages a specific file
        /// </summary>
        /// <param name="fileToStage">The file that is going to be staged</param>
        /// <param name="repo">A pre-existing repository</param>
        public async Task StageFileAsync(ChangedFile fileToStage, Repository repo)
        {
            await Task.Run(() =>
            {
                VerifyPath();

                var status = repo.RetrieveStatus();
                if (!status.IsDirty)
                    return;

                Commands.Stage(repo, fileToStage.FilePath);
            });
        }

        /// <summary>
        /// Unstages a specific file
        /// </summary>
        /// <param name="fileToUnstage">The file that is going to be unstaged</param>
        public async Task UnstageFileAsync(ChangedFile fileToUnstage)
        {
            await Task.Run(() =>
            {
                VerifyPath();

                using var repo = new Repository(_gitInfo?.SavedRepository?.FilePath);
                var status = repo.RetrieveStatus();
                if (!status.IsDirty)
                    return;

                Commands.Unstage(repo, fileToUnstage.FilePath);
            });
        }

        #endregion

        #region Push/Pull

        /// <summary>
        /// Pushes files that are commited to the repository on the specified branch
        /// </summary>
        /// <param name="branch">The branch that files will be pushed to</param>
        private async Task PushFilesAsync(GitBranch branch)
        {
            await Task.Run(() =>
            {
                VerifyPath();

                using var repo = new Repository(_gitInfo?.SavedRepository?.FilePath);

                var remote = repo.Network.Remotes["origin"];
                if (remote == null)
                    throw new InvalidBranchException("No 'origin' remote configured for this repository!");

                var localBranchName = string.IsNullOrEmpty(branch.Name)
                    ? repo.Head.FriendlyName
                    : branch.Name;
                var localBranch = repo.Branches[localBranchName];

                if (localBranch == null)
                    return;

                localBranch = repo.Branches.Update(
                    localBranch,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = localBranch.CanonicalName
                );

                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (_, _, _) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _gitInfo?.PersonalToken,
                            Password = _gitInfo?.PersonalToken,
                        },
                };

                repo.Network.Push(localBranch, pushOptions);
            });
        }

        /// <summary>
        /// Pulls changes from the selected branch
        /// and updates the local repository
        /// </summary>
        /// <returns></returns>
        private async Task<MergeStatus> PullFilesAsync()
        {
            await Task.Run(() =>
            {
                VerifyPath();

                using var repo = new Repository(_gitInfo?.SavedRepository?.FilePath);

                if (repo.Head.TrackedBranch == null)
                {
                    throw new InvalidBranchException(
                        $"'{repo.Head.FriendlyName}' has no upstream branch configured. Push this branch first, or check out a tracked remote branch.");
                }

                var options = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = (_, _, _) =>
                            new UsernamePasswordCredentials
                            {
                                Username = _gitInfo?.PersonalToken,
                                Password = _gitInfo?.PersonalToken,
                            },
                    },
                    MergeOptions = new MergeOptions
                    {
                        FastForwardStrategy = FastForwardStrategy.Default,
                        OnCheckoutNotify = ShowConflict,
                        CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict,
                    }
                };

                //Create signature and pull
                Signature signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                var result = Commands.Pull(repo, signature, options);

                switch (result.Status)
                {
                    //Check for merge conflicts
                    case MergeStatus.Conflicts:
                        //Display in front end eventually
                        return "Conflict detected";
                    case MergeStatus.UpToDate:
                        //Display in front end eventually
                        return "Up to date";
                }

                return "Pulled Successfully";
            });
            return MergeStatus.UpToDate;
        }

        #endregion

        #region Clone

        /// <summary>
        /// Clones a repository at the specified file location
        /// </summary>
        /// <param name="url">The URL for the repository</param>
        /// <param name="cloneLocation">The location to clone to</param>
        public async Task CloneRepositoryAsync(string? url, string? cloneLocation)
        {
            await Task.Run(() =>
            {
                if (cloneLocation == null)
                {
                    throw new NullReferenceException("No path found!");
                }

                if (url == null)
                {
                    throw new NullReferenceException("No URL found!");
                }

                var options = new CloneOptions
                {
                    FetchOptions =
                    {
                        CredentialsProvider = (_, _, _) =>
                            new UsernamePasswordCredentials
                            {
                                Username = _gitInfo?.PersonalToken,
                                Password = _gitInfo?.PersonalToken,
                            },
                    },
                };
                Repository.Clone(url, cloneLocation, options);
            });
        }

        #endregion

        #region Public Method Wrappers

        /// <summary>
        /// Calls GitService to push commited files onto selected branch, handles errors
        /// </summary>
        public async Task PushFiles()
        {
            if (!VerifyCurrentBranch())
                return;

            await PushFilesAsync(CurrentBranch);
        }

        /// <summary>
        /// Pulls changes from the repo and merges them into the local repository
        /// </summary>
        public async Task PullChanges()
        {
            if (!VerifyCurrentBranch())
                return;

            try
            {
                var status = await PullFilesAsync();

                switch (status)
                {
                    //Check for merge conflicts
                    case MergeStatus.Conflicts:
                        //Display in front end eventually
                        OutputToConsole(this, new("Conflict detected"));
                        return;
                    case MergeStatus.UpToDate:
                        //Display in front end eventually
                        OutputToConsole(this, new("Up to date"));
                        return;
                    case MergeStatus.FastForward:
                        OutputToConsole(this, new("Fast forward"));
                        break;
                    case MergeStatus.NonFastForward:
                        OutputToConsole(this, new("Non-fast forward"));
                        break;
                    default:
                        OutputToConsole(this, new("Pulled successfully"));
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                //Notify view models that changes have been pulled
                OnChangesPulled?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        /// <summary>
        /// Opens a dialog to prompt the user to select an already
        /// existing repository to open, or a directory to clone to
        /// </summary>
        /// <returns>True if the file location is empty, otherwise false</returns>
        public Tuple<string, bool> ChooseRepositoryDialog()
        {
            //Open dialog, choose path, check path validity, if path is valid save to user info, if not give message

            //Open file select dialogue
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            //If dialog closes, check result
            if (dialog.ShowDialog() != true) return new Tuple<string, bool>("", false);

            var selectedFilePath = dialog.FolderName;

            try
            {
                //Empty directory and directory isn't a repository
                if (!Repository.IsValid(selectedFilePath))
                {
                    //If directory is empty, return true, otherwise return false
                    return !Directory.EnumerateFiles(selectedFilePath).Any()
                        ? new Tuple<string, bool>(selectedFilePath, true)
                        : new Tuple<string, bool>(selectedFilePath, false);
                }

                //Try to open repository if one already exists
                _ = OpenLocalRepository(selectedFilePath);
            }
            catch (RepositoryNotFoundException ex)
            {
                Trace.WriteLine(ex.Message);
                return new Tuple<string, bool>(selectedFilePath, true);
            }

            return new Tuple<string, bool>(selectedFilePath, false);
        }

        /// <summary>
        /// Verifies that a repository exists at the file location and opens it
        /// </summary>
        /// <param name="filePath">Filepath to a local git repository</param>
        /// <exception cref="RepositoryNotFoundException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        private async Task OpenLocalRepository(string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    var repo = new Repository(filePath);

                    //Set active repo as locally opened repo
                    var remote = repo.Network.Remotes["origin"];
                    if (remote != null)
                    {
                        _gitInfo?.SetUrl(remote.Url);
                    }
                    else
                    {
                        throw new NullReferenceException("Couldn't find any remotes!");
                    }

                    //Save to user info
                    _gitInfo?.SetPath(filePath);
                    JsonDataManager.SaveUserInfo(_gitInfo);
                });

                //Notify view models that the repository data has changed
                OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Clones a local repository from a selected URL
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public async Task CloneRepository(string url, string path)
        {
            try
            {
                VerifyPath();

                if (url == string.Empty || path == string.Empty)
                    throw new NullReferenceException("No URL or Path found!");


                //Clone repo using git service
                await CloneRepositoryAsync(url, path);

                await OpenLocalRepository(path);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}