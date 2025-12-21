using System.Diagnostics;
using System.IO;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Utilities;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace BananaGit.Services
{
    /// <summary>
    /// Handles all git commands and operations
    /// </summary>
    public class GitService
    {
        private readonly GitInfoModel? _gitInfo;
        public GitService() 
        {
            JsonDataManager.LoadUserInfo(ref _gitInfo);

            if (_gitInfo == null)
            {
                throw new LoadDataException("ERROR: Couldn't load user info!");
            }
        }

        #region Getters

        /// <summary>
        /// Checks if the current repo has files commited but not pushed
        /// </summary>
        /// <returns>Whether localally commited files have been pushed</returns>
        public bool HasLocalCommitedFiles()
        {
            using var repo = new Repository(_gitInfo?.GetPath());

            //Check if branch has a remote tracking branch
            if (repo.Head.TrackedBranch == null)
            {
                return false; 
            }
            
            //Start at local head tip and query until remote head tip
            var localCommits = repo.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = repo.Head.Tip.Id,
                ExcludeReachableFrom = repo.Head.TrackedBranch.Tip.Id
            });
            
            bool i = localCommits.Any();
            
            return localCommits.Any();
        }
        #endregion
        
        #region Helper Methods

        /// <summary>
        /// Removes all local commits and reverts to the remotes last commit
        /// (Aka deletes all unpushed commits)
        /// </summary>
        public void ResetLocalCommits()
        {
            using var repo = new Repository(_gitInfo?.GetPath());
            
            //Move HEAD to remotes last commit
            repo.Reset(ResetMode.Hard, repo.Head.TrackedBranch.Tip);
        }
        
        /// <summary>
        /// Checks current repositories file location before using it
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="RepoLocationException"></exception>
        private void VerifyPath(string? path)
        {
            if (_gitInfo?.SavedRepository?.FilePath == null || _gitInfo.SavedRepository?.FilePath == "")
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
        public void CommitStagedFiles(string commitMessage)
        {
            Task.Run(() =>
            {
                VerifyPath(_gitInfo?.SavedRepository?.FilePath);

                using var repo = new Repository(_gitInfo?.SavedRepository?.FilePath);

                //Set author for commiting
                Signature author = new(_gitInfo?.Username, _gitInfo?.Email, DateTime.Now);
                repo.Commit(commitMessage, author, author);
            });
        }

        /// <summary>
        /// Stages all changed files in the working directory
        /// </summary>
        public void StageFiles()
        {
            Task.Run(() =>
            {
                VerifyPath(_gitInfo?.SavedRepository?.FilePath);

                using (var repo = new Repository(_gitInfo?.SavedRepository?.FilePath))
                {
                    //Get status and return if no changes have been made
                    var status = repo.RetrieveStatus();
                    if (!status.IsDirty) return;

                    foreach (var file in status)
                    {
                        if (file.State == FileStatus.Ignored) continue;

                        Commands.Stage(repo, file.FilePath);
                    }
                }
            });
        }

        /// <summary>
        /// Stages a specific file
        /// </summary>
        /// <param name="fileToStage">The file that is going to be staged</param>
        public void StageFile(ChangedFile fileToStage)
        {
            Task.Run(() =>
            {
                VerifyPath(_gitInfo?.SavedRepository?.FilePath);

                using (var repo = new Repository(_gitInfo?.SavedRepository?.FilePath))
                {
                    var status = repo.RetrieveStatus();
                    //if (!status.IsDirty) return;

                    Commands.Stage(repo, fileToStage.FilePath);
                }
            });
        }

        /// <summary>
        /// Unstages a specific file
        /// </summary>
        /// <param name="fileToUnstage">The file that is going to be unstaged</param>
        public void UnstageFile(ChangedFile fileToUnstage)
        {
            Task.Run(() =>
            {
                VerifyPath(_gitInfo?.SavedRepository?.FilePath);

                using (var repo = new Repository(_gitInfo?.SavedRepository?.FilePath))
                {
                    var status = repo.RetrieveStatus();
                    if (!status.IsDirty) return;

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
        public void PushFiles(GitBranch branch)
        {
            Task.Run(() =>
            {
                VerifyPath(_gitInfo?.SavedRepository?.FilePath);

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
                        throw new InvalidBranchException("Invalid branch inputted! Cannot push files!");

                    //User credentials
                    FetchOptions options = new FetchOptions
                    {
                        CredentialsProvider = (url, username, types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _gitInfo?.Username,
                            Password = _gitInfo?.PersonalToken
                        }
                    };

                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, options, string.Empty);

                    var localBranchName = string.IsNullOrEmpty(branch.Name) ? repo.Head.FriendlyName : branch.Name;
                    var localBranch = repo.Branches[localBranchName];

                    if (localBranch == null) return;

                    repo.Branches.Update(localBranch,
                        b => b.Remote = remote.Name,
                        b => b.UpstreamBranch = localBranch.CanonicalName);

                    var pushOptions = new PushOptions
                    {
                        CredentialsProvider = (url, username, types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _gitInfo?.Username,
                            Password = _gitInfo?.PersonalToken
                        }
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
        public MergeStatus PullFiles(GitBranch branch)
        {
            Task.Run(() => {
                VerifyPath(_gitInfo?.SavedRepository?.FilePath);

                var options = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = new CredentialsHandler((url, username, types) => new UsernamePasswordCredentials
                        {
                            Username = _gitInfo?.Username,
                            Password = _gitInfo?.PersonalToken
                        })
                    }
                };

                options.MergeOptions = new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.Default,
                    OnCheckoutNotify = new CheckoutNotifyHandler(ShowConflict),
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict
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
        /*/// <summary>
        /// Prompts the user with a windows dialog to select a directory
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NullReferenceException"></exception>
        public Tuple<string,string> ChooseCloneDirectory()
        {
              //Open file select dialogue
                OpenFolderDialog dialog = new OpenFolderDialog
                {
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                };

                if (dialog.ShowDialog() != true)
                    throw new Exception("Please select a folder!");
                
                string path;
                string url = string.Empty;
                
                string selectedFilePath = dialog.FolderName;

                //Check if directory is empty
                if (!Directory.EnumerateFiles(selectedFilePath).Any())
                {
                    _gitInfo?.SetPath(dialog.FolderName);
                    path = dialog.FolderName;
                }
                else
                {
                    //Check if file location is local repo
                    using (var repo = new Repository(selectedFilePath))
                    {
                        //Set active repo as locally opened repo
                        _gitInfo?.SetPath(dialog.FolderName);
                        path = dialog.FolderName;
                        var remote = repo.Network.Remotes.FirstOrDefault();
                        if (remote != null)
                        {
                            _gitInfo?.SetUrl(remote.Url);
                            url = remote.Url;
                        }
                        else
                        {
                            throw new NullReferenceException("Couldn't find any remotes!");
                        }
                        
                        //Save updated repository information
                        JsonDataManager.SaveUserInfo(_gitInfo);
                    }
                }
                return new Tuple<string, string>(path, url);
        }*/

        /// <summary>
        /// Clones a repository at the specified file location
        /// </summary>
        /// <param name="url">The URL for the repository</param>
        /// <param name="cloneLocation">The location to clone to</param>
        public void CloneRepository(string url, string cloneLocation)
        {
            var options = new CloneOptions
            {
                FetchOptions =
                {
                    CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
                    {
                        Username = _gitInfo?.Username,
                        Password = _gitInfo?.PersonalToken
                    }
                }
            };
            Repository.Clone(url, cloneLocation, options);
        }
        #endregion
    }
}
