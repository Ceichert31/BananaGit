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

            if (!Directory.Exists(LocalRepoFilePath))
            {
                throw new RepoLocationException("Local repository file path is missing!");
            }
        }
        #endregion

        #region Stage/Commit
        /// <summary>
        /// Commits all staged files off of the main thread
        /// </summary>
        /// <param name="commitMessage">The message for this commit</param>
        public void CommitStagedFilesAsync(string commitMessage)
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

        public void StageFilesAsync()
        {
            Task.Run(() =>
            {

            });
        }

       

        [RelayCommand]
        public void StageFiles()
        {
            Task.Run(() =>
            {
                try
                {
                    VerifyPath(LocalRepoFilePath);

                    using var repo = new Repository(LocalRepoFilePath);

                    var status = repo.RetrieveStatus();
                    if (!status.IsDirty) return;


                    foreach (var file in status)
                    {
                        if (file.State == FileStatus.Ignored) continue;

                        Commands.Stage(repo, file.FilePath);
                    }
                }
                catch (LibGit2SharpException ex)
                {
                    OutputError($"Failed to stage {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    OutputError(ex.Message);
                }
            });
        }

        [RelayCommand]
        public void StageFile(ChangedFile file)
        {
            try
            {
                VerifyPath(LocalRepoFilePath);

                using var repo = new Repository(LocalRepoFilePath);

                var status = repo.RetrieveStatus();
                if (!status.IsDirty) return;

                Commands.Stage(repo, file.FilePath);
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to stage {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }

        [RelayCommand]
        public void UnstageFile(ChangedFile file)
        {
            try
            {
                VerifyPath(LocalRepoFilePath);

                using var repo = new Repository(LocalRepoFilePath);

                var status = repo.RetrieveStatus();
                if (!status.IsDirty) return;

                Commands.Unstage(repo, file.FilePath);
            }
            catch (LibGit2SharpException ex)
            {
                OutputError($"Failed to stage {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                OutputError(ex.Message);
            }
        }
        #endregion
    }
}
