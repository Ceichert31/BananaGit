using BananaGit.Models;
using BananaGit.Services;

namespace BananaGit.Test.Services;

[TestClass]
public class GitServiceTests
{
    private const string TEST_REPO = "https://github.com/Ceichert31/test-repo.git";
    private const string TEST_PATH = "C:/TestRepo/";
    
    //Need clone test b4 commit test so we can clone repo for testing
    
    [TestMethod]
    public async Task TestCommitFiles_ReturnsTrue()
    {
        GitService gitService = new GitService();
        try
        {
            //Arrange
            ChangedFile file = new ChangedFile
            {
                Name = "test.txt",
                FilePath = "C:/TestRepo/"
            };
            await File.Create(file.FilePath + file.Name).DisposeAsync();
            
            await gitService.StageFileAsync(file);
            
            //Act
            await gitService.CommitStagedFilesAsync("Test commit");

            //Assert
            Assert.IsTrue(gitService.HasLocalCommitedFiles());
        }
        finally
        {
            //Cleanup
            _ = gitService.ResetLocalCommitsAsync();
        }
    }
}