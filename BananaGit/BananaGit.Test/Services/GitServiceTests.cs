using BananaGit.Models;
using BananaGit.Services;

namespace BananaGit.Test.Services;

[TestClass]
public class GitServiceTests
{
    private const string TEST_REPO = "https://github.com/Ceichert31/test-repo.git";
    private const string TEST_PATH = "C:/TestRepo/";
    
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
            File.Create(file.FilePath + file.Name).Dispose();
            
            await Task.Run(() => Thread.Sleep(1000));
            
            gitService.StageFile(file);
            
            await Task.Run(() => Thread.Sleep(1000));
            
            //Act
            gitService.CommitStagedFiles("Test commit");
            
            await Task.Run(() => Thread.Sleep(1000));

            //Assert
            Assert.IsTrue(gitService.HasLocalCommitedFiles());
        }
        finally
        {
            //Cleanup
            gitService.ResetLocalCommits();
        }
    }
}