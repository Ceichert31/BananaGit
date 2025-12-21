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
    
    //Need clone test b4 commit test so we can clone repo for testing

    [TestMethod]
    public void TestCloneRepository_ReturnsTrue()
    {
        GitService gitService = new GitService();
        try
        {
            
        }
        finally
        {
            
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
        newUserInfo.SetPath(TEST_PATH);
        newUserInfo.SetUrl(TEST_REPO);
        JsonDataManager.SaveUserInfo(newUserInfo);
        try
        {
            //Arrange
            ChangedFile file = new ChangedFile
            {
                Name = "test.txt",
                FilePath = "C:/TestRepo/"
            };
            await File.Create(file.FilePath + file.Name).DisposeAsync();
            Repository repo = new Repository(TEST_PATH);
            
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