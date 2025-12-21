using BananaGit.Models;
using BananaGit.Services;

namespace BananaGit.Test.Services;

[TestClass]
public class GitServiceTests
{
    [TestMethod]
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
        Assert.IsTrue(gitService.HasLocalCommitedFiles());

        //Cleanup
        gitService.ResetLocalCommits();
    }
}