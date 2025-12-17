using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BananaGit.Exceptions;
using BananaGit.Models;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;

namespace BananaGit.Services
{
    public class GitService
    {
        private GitInfoModel _gitInfo;
        public GitService() 
        {
            JsonDataManager.LoadUserInfo(ref _gitInfo);

            if (_gitInfo == null)
            {
                throw new LoadDataException("ERROR: Couldn't load user info!");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Checks current repositories file location before using it
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="RepoLocationException"></exception>
        private void VerifyPath(string path)
        {
            if (_gitInfo.SavedRepository?.FilePath == null || _gitInfo.SavedRepository?.FilePath == "")
            {
                throw new RepoLocationException("Local repository file path is empty!");
            }

            if (!Directory.Exists(_gitInfo.SavedRepository.FilePath))
            {
                throw new RepoLocationException("Local repository file path is missing!");
            }
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
                VerifyPath(_gitInfo.SavedRepository?.FilePath);

                using var repo = new Repository(_gitInfo.SavedRepository?.FilePath);

                //Set author for commiting
                Signature author = new(_gitInfo?.Username, _gitInfo?.Email, DateTime.Now);
                Signature committer = author;

                repo.Commit(commitMessage, author, committer);
            });
        }

        /// <summary>
        /// Stages all changed files in the working directory
        /// </summary>
        public void StageFiles()
        {
            Task.Run(() =>
            {
                VerifyPath(_gitInfo.SavedRepository?.FilePath);

                using (var repo = new Repository(_gitInfo.SavedRepository?.FilePath))
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
                VerifyPath(_gitInfo.SavedRepository?.FilePath);

                using (var repo = new Repository(_gitInfo.SavedRepository.FilePath))
                {
                    var status = repo.RetrieveStatus();
                    if (!status.IsDirty) return;

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
                VerifyPath(_gitInfo.SavedRepository?.FilePath);

                using (var repo = new Repository(_gitInfo.SavedRepository.FilePath))
                {
                    var status = repo.RetrieveStatus();
                    if (!status.IsDirty) return;

                    Commands.Unstage(repo, fileToStage.FilePath);
                }
            });
        }
        #endregion
    }
}
