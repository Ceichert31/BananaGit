using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using LibGit2Sharp;

namespace BananaGit.Test.Services;

[TestClass]
public class GitServiceTests
{
    private const string TEST_REPO = "https://github.com/Ceichert31/test-repo.git";
    private const string TEST_PATH_BASE = "C:/UnitTestRepositories/";
    
    //For writing tests for the git service, this is the general setup:
    /*
      //Arrange
        GitService gitService = new GitService();
        try
        {
            //Act
            
            //Assert
        }
        finally
        {
            //Cleanup
        }
     */

    [TestMethod]
    public async Task TestCloneRepository_ReturnsTrue()
    {
        //Arrange
        GitService gitService = new GitService();
        string testPath = Path.Combine(TEST_PATH_BASE, "TestCloneRepository");
        try
        {
            //Repository exists
            if (Directory.Exists(testPath))
            {
                DeleteDirectory(testPath);
            }
            
            //Act
            await gitService.CloneRepositoryAsync(TEST_REPO, testPath);
            
            //Assert
            Assert.IsTrue(Directory.Exists(testPath));
            Assert.IsTrue(Directory.EnumerateFiles(testPath).Any());
        }
        finally
        {
            DeleteDirectory(testPath);
        }
    }
    
    [TestMethod]
    public async Task TestCommitFiles_ReturnsTrue()
    {
        GitService gitService = new GitService();
        string testPath = Path.Combine(TEST_PATH_BASE, "TestCommitFiles");
        
        //Cache old info
        GitInfoModel? oldUserInfo = new GitInfoModel();
        GitInfoModel newUserInfo = new GitInfoModel();
        JsonDataManager.LoadUserInfo(ref oldUserInfo);        
        
        //Set testing info
        newUserInfo.CopyContents(oldUserInfo);
        newUserInfo.SavedRepository = new SavableRepository(testPath, TEST_REPO);
        JsonDataManager.SaveUserInfo(newUserInfo);
        try
        {
            //Arrange
            DeleteDirectory(testPath);
            await gitService.CloneRepositoryAsync(TEST_REPO, testPath);
            
            //Create and add file to stage
            ChangedFile file = new ChangedFile
            {
                Name = "test.txt",
                FilePath = Path.Combine(testPath, "test.txt")
            };
            await File.Create(file.FilePath).DisposeAsync();
            using Repository repo = new Repository(testPath);
            
            //Stage file
            await gitService.StageFileAsync(file, repo);
            
            //Act
            await gitService.CommitStagedFilesAsync("Test commit");
            
            //Assert
            Assert.IsTrue(gitService.HasLocalCommitedFiles());
        }
        finally
        {
            //Cleanup
            await gitService.ResetLocalCommitsAsync();
            DeleteDirectory(testPath);
            
            //Reset back to non-test repository info
            JsonDataManager.SaveUserInfo(oldUserInfo);
        }
    }
    
    /// <summary>
    /// Deletes a directory completely including read-only and hidden items
    /// </summary>
    /// <param name="path"></param>
    private void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        
        var directory = new DirectoryInfo(path);

        foreach (var file in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }
        directory.Attributes = FileAttributes.Normal;
        directory.Delete(true);
    }
}