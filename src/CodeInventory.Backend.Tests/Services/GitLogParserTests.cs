using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using CodeInventory.Backend.Services;

namespace CodeInventory.Backend.Tests.Services;

[TestFixture]
public class GitLogParserTests
{
    private ILogger<GitLogParser> _logger;
    private GitLogParser _parser;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<GitLogParser>>();
        _parser = new GitLogParser(_logger);
    }

    [Test]
    public void ParseGitLogOutput_WithValidSingleCommit_ReturnsParsedCommit()
    {
        // Arrange
        var gitLogOutput = "abc123def456789012345678901234567890abcd|||John Doe|||john@example.com|||2024-01-15T10:30:00+00:00|||Initial commit";

        // Act
        var result = _parser.ParseGitLogOutput(gitLogOutput);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        var commit = result.First();
        Assert.That(commit.Sha, Is.EqualTo("abc123def456789012345678901234567890abcd"));
        Assert.That(commit.AuthorName, Is.EqualTo("John Doe"));
        Assert.That(commit.AuthorEmail, Is.EqualTo("john@example.com"));
        Assert.That(commit.Message, Is.EqualTo("Initial commit"));
        Assert.That(commit.AuthorTimestamp.Year, Is.EqualTo(2024));
    }

    [Test]
    public void ParseGitLogOutput_WithMultipleCommits_ReturnsAllCommits()
    {
        // Arrange
        var gitLogOutput = 
            "abc123def456789012345678901234567890abcd|||John Doe|||john@example.com|||2024-01-15T10:30:00+00:00|||Initial commit\n" +
            "def456abc123789012345678901234567890abcd|||Jane Smith|||jane@example.com|||2024-01-16T14:45:00+00:00|||Add feature X";

        // Act
        var result = _parser.ParseGitLogOutput(gitLogOutput);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().AuthorName, Is.EqualTo("John Doe"));
        Assert.That(result.Last().AuthorName, Is.EqualTo("Jane Smith"));
    }

    [Test]
    public void ParseGitLogOutput_WithEmptyInput_ReturnsEmptyCollection()
    {
        // Arrange
        var gitLogOutput = "";

        // Act
        var result = _parser.ParseGitLogOutput(gitLogOutput);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseGitLogOutput_WithInvalidShaFormat_SkipsInvalidCommit()
    {
        // Arrange
        var gitLogOutput = "invalid-sha|||John Doe|||john@example.com|||2024-01-15T10:30:00+00:00|||Invalid commit";

        // Act
        var result = _parser.ParseGitLogOutput(gitLogOutput);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseGitLogOutput_WithInsufficientParts_SkipsInvalidLine()
    {
        // Arrange
        var gitLogOutput = "abc123def456789012345678901234567890abcd|||John Doe|||john@example.com"; // Missing timestamp and message

        // Act
        var result = _parser.ParseGitLogOutput(gitLogOutput);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseInitialCommitSha_WithValidSha_ReturnsSha()
    {
        // Arrange
        var gitRevListOutput = "abc123def456789012345678901234567890abcd\n";

        // Act
        var result = _parser.ParseInitialCommitSha(gitRevListOutput);

        // Assert
        Assert.That(result, Is.EqualTo("abc123def456789012345678901234567890abcd"));
    }

    [Test]
    public void ParseInitialCommitSha_WithEmptyInput_ReturnsNull()
    {
        // Arrange
        var gitRevListOutput = "";

        // Act
        var result = _parser.ParseInitialCommitSha(gitRevListOutput);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseInitialCommitSha_WithInvalidShaFormat_ReturnsNull()
    {
        // Arrange
        var gitRevListOutput = "invalid-sha-format";

        // Act
        var result = _parser.ParseInitialCommitSha(gitRevListOutput);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseGitLogOutput_WithCommitMessageContainingDelimiter_ParsesCorrectly()
    {
        // Arrange
        var gitLogOutput = "abc123def456789012345678901234567890abcd|||John Doe|||john@example.com|||2024-01-15T10:30:00+00:00|||Fix bug: handle ||| in message";

        // Act
        var result = _parser.ParseGitLogOutput(gitLogOutput);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        var commit = result.First();
        Assert.That(commit.Message, Is.EqualTo("Fix bug: handle ||| in message"));
    }
}