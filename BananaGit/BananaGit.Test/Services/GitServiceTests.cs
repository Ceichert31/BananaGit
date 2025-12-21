using BananaGit.Models;
using BananaGit.Services;

namespace BananaGit.Test.Services;

public class GitServiceTests
{
    [Test]
    public void TestCommitFiles_ReturnsTrue()
    {
        //Arrange
        GitService gitService = new GitService();
        ChangedFile file = new ChangedFile
        {
            Name = "test.txt",
            FilePath = "testFolder/"  
        };
        gitService.StageFile(file);
        
        //Act
        gitService.CommitStagedFiles("Test commit");
        
        //Assert
        Assert.Equals(gitService.HasLocalCommitedFiles(), true);
        
        //Cleanup
        gitService.ResetLocalCommits();
    }
}