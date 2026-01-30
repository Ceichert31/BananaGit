using BananaGit.Models;
using BananaGit.Services;
using BananaGit.Utilities;
using LibGit2Sharp;
using Microsoft.Extensions.Configuration;

namespace BananaGit.Test.Services;

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

[TestClass]
public class GitServiceTests
{
    private const string TEST_REPO = "https://github.com/Ceichert31/test-repo.git";
    private const string TEST_PATH_BASE = "C:/UnitTestRepositories/";
    
    private static GitInfoModel? _userInfo;
    
    /// <summary>
    /// Loads GitHub secrets user data
    /// </summary>
    /// <param name="testContext"></param>
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        //Attempt to get user info from local dev, then CI/CD
        var config = new ConfigurationBuilder()
            .AddUserSecrets<GitServiceTests>()
            .AddEnvironmentVariables()
            .Build();
        
        _userInfo = new GitInfoModel()
        {
            Username = config["GIT_TEST_USERNAME"],
            Email = config["GIT_TEST_EMAIL"],
            PersonalToken =  config["GIT_TEST_TOKEN"],
            SavedRepository = new SavableRepository("", TEST_REPO)
        };
    }

    [TestMethod]
    public async Task TestCloneRepository_ReturnsTrue()
    {
        //Arrange
        string testPath = Path.Combine(TEST_PATH_BASE, "TestCloneRepository");
        _userInfo?.SetPath(testPath);
        GitService gitService = new GitService(_userInfo);
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
        string testPath = Path.Combine(TEST_PATH_BASE, "TestCommitFiles");
        _userInfo?.SetPath(testPath);
        GitService gitService = new GitService(_userInfo);

        ChangedFile file = new ChangedFile(gitService)
        {
            Name = "test.txt",
            FilePath = Path.Combine(testPath, "test.txt")
        };
        try
        {
            //Arrange
            DeleteDirectory(testPath);
            await gitService.CloneRepositoryAsync(TEST_REPO, testPath);
            
            //Create and add file to stage
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