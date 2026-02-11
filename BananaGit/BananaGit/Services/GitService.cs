using System.Diagnostics;
using System.IO;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Utilities;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
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

        public GitService(GitInfoModel? gitInfo)
        {
            _gitInfo = gitInfo;
            JsonDataManager.UserInfoChanged += OnUserDataChange;
        }

        /// <summary>
        /// Updates the current user info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUserDataChange(object? sender, EventArgs e)
        {
            JsonDataManager.LoadUserInfo(ref _gitInfo);
        }

        #region Getters
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
                localBranches.Add(new GitBranch(branch));
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
                remoteBranches.Add(new GitBranch(branch));
            }
            return remoteBranches;
        }

        #endregion

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
        /// Returns the commit history for the current branch
        /// </summary>
        /// <returns>A list of past commits</returns>
        public List<GitCommitInfo> GetCommitHistory(int historyLength)
        {
            VerifyPath();

            using var repo = new Repository(_gitInfo?.GetPath());

            var commits = repo.Branches[_gitInfo?.CurrentBranch?.CanonicalName].Commits.ToList();

            List<GitCommitInfo> commitList = new();

            if (historyLength > commits.Count)
            {
                historyLength = commits.Count;
            }
            
            //Iterate through and convert commit info into GitCommitInfo model
            for (int i = 0; i < historyLength; i++)
            {
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
                
                repo.CheckoutPaths("HEAD", new[] {filePath}, new CheckoutOptions
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
            if (
                _gitInfo?.SavedRepository?.FilePath == null
                || _gitInfo.SavedRepository?.FilePath == ""
            )
            {
                throw new RepoLocationException("Local repository file path is empty!");
            }

            if (!Directory.Exists(_gitInfo?.SavedRepository?.FilePath))
            {
                throw new RepoLocationException("Local repository file path is missing!");
            }
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

                using (var repo = new Repository(_gitInfo?.SavedRepository?.FilePath))
                {
                    var status = repo.RetrieveStatus();
                    if (!status.IsDirty)
                        return;

                    Commands.Unstage(repo, fileToUnstage.FilePath);
                }
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

                using (var repo = new Repository(_gitInfo?.SavedRepository?.FilePath))
                {
                    var remote = repo.Network.Remotes[branch.Name];
                    if (remote != null)
                    {
                        repo.Network.Remotes.Remove(branch.Name);
                    }

                    repo.Network.Remotes.Add(branch.Name, _gitInfo?.SavedRepository?.Url);
                    remote = repo.Network.Remotes[branch.Name];

                    if (remote == null)
                        throw new InvalidBranchException(
                            "Invalid branch inputted! Cannot push files!"
                        );

                    //User credentials
                    FetchOptions options = new FetchOptions
                    {
                        CredentialsProvider = (url, username, types) =>
                            new UsernamePasswordCredentials
                            {
                                Username = _gitInfo?.Username,
                                Password = _gitInfo?.PersonalToken,
                            },
                    };

                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, options, string.Empty);

                    var localBranchName = string.IsNullOrEmpty(branch.Name)
                        ? repo.Head.FriendlyName
                        : branch.Name;
                    var localBranch = repo.Branches[localBranchName];

                    if (localBranch == null)
                        return;

                    repo.Branches.Update(
                        localBranch,
                        b => b.Remote = remote.Name,
                        b => b.UpstreamBranch = localBranch.CanonicalName
                    );

                    var pushOptions = new PushOptions
                    {
                        CredentialsProvider = (url, username, types) =>
                            new UsernamePasswordCredentials
                            {
                                Username = _gitInfo?.Username,
                                Password = _gitInfo?.PersonalToken,
                            },
                    };

                    repo.Network.Push(localBranch, pushOptions);
                }
            });
        }

        /// <summary>
        /// Pulls changes from the selected branch
        /// and updates the local repository
        /// </summary>
        /// <param name="branch">The branch changes will be pulled from</param>
        /// <returns></returns>
        private async Task<MergeStatus> PullFilesAsync(GitBranch branch)
        {
            await Task.Run(() =>
            {
                VerifyPath();

                var options = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = new CredentialsHandler(
                            (url, username, types) =>
                                new UsernamePasswordCredentials
                                {
                                    Username = _gitInfo?.Username,
                                    Password = _gitInfo?.PersonalToken,
                                }
                        ),
                    },
                };

                options.MergeOptions = new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.Default,
                    OnCheckoutNotify = new CheckoutNotifyHandler(ShowConflict),
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict,
                };

                using (var repo = new Repository(_gitInfo?.SavedRepository?.FilePath))
                {
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
                if (cloneLocation == null )
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
                                Username = _gitInfo?.Username,
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
            try
            {
                if (_gitInfo?.CurrentBranch == null)
                    throw new NullReferenceException("No Branch selected! Branch is null!");

                await PushFilesAsync(_gitInfo.CurrentBranch);
            }
            catch (LibGit2SharpException ex)
            {
                Trace.WriteLine($"Failed to Push {ex.Message}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Pulls changes from the repo and merges them into the local repository
        /// </summary>
        public async Task PullChanges()
        {
            try
            {
                if (_gitInfo?.CurrentBranch == null)
                    throw new NullReferenceException("No Branch selected! Branch is null!");

                var status = await PullFilesAsync(_gitInfo.CurrentBranch);

                switch (status)
                {
                    //Check for merge conflicts
                    case MergeStatus.Conflicts:
                        //Display in front end eventually
                        Trace.WriteLine("Conflict detected");
                        return;
                    case MergeStatus.UpToDate:
                        //Display in front end eventually
                        Trace.WriteLine("Up to date");
                        return;
                    case MergeStatus.FastForward:
                        Trace.WriteLine("Fast Forward");
                        break;
                    case MergeStatus.NonFastForward:
                        Trace.WriteLine("Non-Fast Forward");
                        break;
                    default:
                        Trace.WriteLine("Pulled Successfully");
                        break;
                }
            }
            catch (NullReferenceException ex)
            {
                Trace.WriteLine(ex.Message);

                if (_gitInfo != null)
                {
                    _gitInfo.CurrentBranch = new GitBranch(_gitInfo);
                    JsonDataManager.SaveUserInfo(_gitInfo);
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
        public bool ChooseRepositoryDialog()
        {
            //Open dialog, choose path, check path validity, if path is valid save to user info, if not give message
            try
            {
                //Open file select dialogue
                OpenFolderDialog dialog = new OpenFolderDialog
                {
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                //If dialog closes, check result
                if (dialog.ShowDialog() != true) return false;
                
                string selectedFilePath = dialog.FolderName;

                //Check if directory is empty and mark as cloneable
                if (!Directory.EnumerateFiles(selectedFilePath).Any())
                {
                    //Directory is empty, can clone
                    /*CanClone = true;
                    LocalRepoFilePath = selectedFilePath;
                    DirectoryHasFiles = false;*/
                    _gitInfo?.SetPath(selectedFilePath);
                    JsonDataManager.SaveUserInfo(_gitInfo);
                    return true;
                }
                //Otherwise open if a repository already exists there
                OpenLocalRepository(selectedFilePath);
            }
            catch (Exception ex)
            {
                /*CanClone = false;
                NoRepoCloned = true;
                DirectoryHasFiles = true;*/
                Trace.WriteLine(ex.Message);
            }
            return true;
        }
        
        /// <summary>
        /// Verifies that a repository exists at the file location and opens it
        /// </summary>
        /// <param name="filePath">Filepath to a local git repository</param>
        /// <exception cref="RepositoryNotFoundException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        private void OpenLocalRepository(string filePath)
        {
            try
            {
                Task.Run(() =>
                {
                    //Check if file location is local repo
                    if (!Repository.IsValid(filePath)) 
                        throw new RepositoryNotFoundException($"Repository not found at {filePath}!");
            
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
                    
                    //Notify view models that the repository data has changed
                    OnRepositoryChanged?.Invoke(this, EventArgs.Empty);
                });
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
        public async Task CloneRepository()
        {
            try
            { 
                VerifyPath();

                string path = _gitInfo?.GetPath() ?? throw new NullReferenceException("No path found!");
                
                //Clone repo using git service
                await CloneRepositoryAsync(_gitInfo?.GetUrl(),
                    path);
                
                OpenLocalRepository(path);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
   }
}
