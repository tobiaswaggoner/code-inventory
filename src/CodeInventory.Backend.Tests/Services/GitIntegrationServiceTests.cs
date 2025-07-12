using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using CodeInventory.Backend.Services;
using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Tests.Services;

[TestFixture]
public class GitIntegrationServiceTests
{
    private ILogger<GitIntegrationService> _logger;
    private IGitCommandService _gitCommandService;
    private IGitLogParser _gitLogParser;
    private GitIntegrationService _service;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<GitIntegrationService>>();
        _gitCommandService = Substitute.For<IGitCommandService>();
        _gitLogParser = Substitute.For<IGitLogParser>();
        _service = new GitIntegrationService(_logger, _gitCommandService, _gitLogParser);
    }

    [Test]
    public async Task ExtractProjectDataAsync_WithNullPath_ReturnsNull()
    {
        // Act
        var result = await _service.ExtractProjectDataAsync(null!);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ExtractProjectDataAsync_WithEmptyPath_ReturnsNull()
    {
        // Act
        var result = await _service.ExtractProjectDataAsync("");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ExtractProjectDataAsync_WithNonExistentPath_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = "/path/that/does/not/exist";

        // Act
        var result = await _service.ExtractProjectDataAsync(nonExistentPath);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ExtractProjectDataAsync_WithNonGitRepository_ReturnsNull()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, "test-non-git");
        
        try
        {
            Directory.CreateDirectory(testDir);

            // Mock git command service to return false for IsGitRepository
            _gitCommandService.IsGitRepositoryAsync(testDir, Arg.Any<CancellationToken>())
                .Returns(false);

            // Act
            var result = await _service.ExtractProjectDataAsync(testDir);

            // Assert
            Assert.That(result, Is.Null);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }
    }

    [Test]
    public async Task ExtractProjectDataAsync_WithValidGitRepository_ReturnsProjectData()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, "test-git-repo");
        
        try
        {
            Directory.CreateDirectory(testDir);

            // Mock git command service
            _gitCommandService.IsGitRepositoryAsync(testDir, Arg.Any<CancellationToken>())
                .Returns(true);

            // Mock git rev-list command result
            var initialCommitResult = new GitCommandResult
            {
                IsSuccess = true,
                Output = "abc123def456789012345678901234567890abcd"
            };
            _gitCommandService.ExecuteGitCommandAsync(testDir, "rev-list --max-parents=0 HEAD", Arg.Any<CancellationToken>())
                .Returns(initialCommitResult);

            // Mock git log command result
            var gitLogResult = new GitCommandResult
            {
                IsSuccess = true,
                Output = "abc123def456789012345678901234567890abcd|||John Doe|||john@example.com|||2024-01-15T10:30:00+00:00|||Initial commit"
            };
            _gitCommandService.ExecuteGitCommandAsync(testDir, "log --all --pretty=format:\"%H|||%an|||%ae|||%aI|||%s\" --no-patch", Arg.Any<CancellationToken>())
                .Returns(gitLogResult);

            // Mock git status command result
            var statusResult = new GitCommandResult
            {
                IsSuccess = true,
                Output = ""
            };
            _gitCommandService.ExecuteGitCommandAsync(testDir, "status --porcelain", Arg.Any<CancellationToken>())
                .Returns(statusResult);

            // Mock parser results
            _gitLogParser.ParseInitialCommitSha("abc123def456789012345678901234567890abcd")
                .Returns("abc123def456789012345678901234567890abcd");

            var mockCommits = new List<GitCommitData>
            {
                new GitCommitData
                {
                    Sha = "abc123def456789012345678901234567890abcd",
                    AuthorName = "John Doe",
                    AuthorEmail = "john@example.com",
                    AuthorTimestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                    Message = "Initial commit"
                }
            };
            _gitLogParser.ParseGitLogOutput(Arg.Any<string>())
                .Returns(mockCommits);

            // Act
            var result = await _service.ExtractProjectDataAsync(testDir);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ProjectName, Is.EqualTo("test-git-repo"));
            Assert.That(result.InitialCommitSha, Is.EqualTo("abc123def456789012345678901234567890abcd"));
            Assert.That(result.HasUncommittedChanges, Is.False);
            Assert.That(result.Commits.Count(), Is.EqualTo(1));
            Assert.That(result.Path, Is.EqualTo(testDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }
    }

    [Test]
    public async Task CheckUncommittedChangesAsync_WithCleanRepository_ReturnsFalse()
    {
        // Arrange
        var testPath = "/test/path";
        var statusResult = new GitCommandResult
        {
            IsSuccess = true,
            Output = ""
        };
        _gitCommandService.ExecuteGitCommandAsync(testPath, "status --porcelain", Arg.Any<CancellationToken>())
            .Returns(statusResult);

        // Act
        var result = await _service.CheckUncommittedChangesAsync(testPath);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CheckUncommittedChangesAsync_WithUncommittedChanges_ReturnsTrue()
    {
        // Arrange
        var testPath = "/test/path";
        var statusResult = new GitCommandResult
        {
            IsSuccess = true,
            Output = " M modified_file.txt\n?? untracked_file.txt"
        };
        _gitCommandService.ExecuteGitCommandAsync(testPath, "status --porcelain", Arg.Any<CancellationToken>())
            .Returns(statusResult);

        // Act
        var result = await _service.CheckUncommittedChangesAsync(testPath);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetCommitHistoryAsync_WithFailedGitCommand_ReturnsEmptyCollection()
    {
        // Arrange
        var testPath = "/test/path";
        var failedResult = new GitCommandResult
        {
            IsSuccess = false,
            Error = "Git command failed"
        };
        _gitCommandService.ExecuteGitCommandAsync(testPath, "log --all --pretty=format:\"%H|||%an|||%ae|||%aI|||%s\" --no-patch", Arg.Any<CancellationToken>())
            .Returns(failedResult);

        // Act
        var result = await _service.GetCommitHistoryAsync(testPath);

        // Assert
        Assert.That(result, Is.Empty);
    }
}