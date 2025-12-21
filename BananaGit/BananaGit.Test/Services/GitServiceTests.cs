using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using LibGit2Sharp;

namespace BananaGit.Test.Services;

[TestClass]
public class GitServiceTests
{
    private const string TEST_REPO = "https://github.com/Ceichert31/test-repo.git";
    private const string TEST_PATH = "C:/TestRepo/";
    
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
        try
        {
            //Repository exists
            if (Directory.Exists(TEST_PATH))
            {
                if (Directory.EnumerateFiles(TEST_PATH).Any())
                {
                    Directory.Delete(TEST_PATH, true);
                }
            }
            
            //Act
            await gitService.CloneRepositoryAsync(TEST_REPO, TEST_PATH);
            
            //Assert
            Assert.IsTrue(Directory.Exists(TEST_PATH));
            Assert.IsTrue(Directory.EnumerateFiles(TEST_PATH).Any());
        }
        finally
        {
            /*var directory = new DirectoryInfo(TEST_PATH)
            {
                Attributes = FileAttributes.Normal
            };
            foreach (var file in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(file.FullName);
                fileInfo.Attributes = FileAttributes.Normal;
                file.Delete();  
            }
            directory.Delete(true);*/
        }
    }
    
    [TestMethod]
    public async Task TestCommitFiles_ReturnsTrue()
    {
        GitService gitService = new GitService();
        
        //Cache old info
        GitInfoModel? oldUserInfo = new GitInfoModel();
        GitInfoModel newUserInfo = new GitInfoModel();
        JsonDataManager.LoadUserInfo(ref oldUserInfo);        
        
        //Set testing info
        newUserInfo.CopyContents(oldUserInfo);
        newUserInfo.SavedRepository = new SavableRepository(TEST_PATH, TEST_REPO);
        JsonDataManager.SaveUserInfo(newUserInfo);
        try
        {
            //Arrange
            //Clone test repo if it doesn't already exist
            if (!Directory.EnumerateFiles(TEST_PATH).Any())
            {
                await gitService.CloneRepositoryAsync(TEST_REPO, TEST_PATH);
            }
            
            //Create and add file to stage
            ChangedFile file = new ChangedFile
            {
                Name = "test.txt",
                FilePath = "C:/TestRepo/"
            };
            await File.Create(file.FilePath + file.Name).DisposeAsync();
            Repository repo = new Repository(TEST_PATH);
            
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
            
            //Reset back to non-test repository info
            JsonDataManager.SaveUserInfo(oldUserInfo);
        }
    }
}