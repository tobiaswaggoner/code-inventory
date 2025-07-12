using System.Globalization;
using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class GitLogParser : IGitLogParser
{
    private readonly ILogger<GitLogParser> _logger;
    private const string LogDelimiter = "|||";

    public GitLogParser(ILogger<GitLogParser> logger)
    {
        _logger = logger;
    }

    public IEnumerable<GitCommitData> ParseGitLogOutput(string gitLogOutput)
    {
        if (string.IsNullOrWhiteSpace(gitLogOutput))
        {
            _logger.LogDebug("Git log output is empty or whitespace");
            return Enumerable.Empty<GitCommitData>();
        }

        var commits = new List<GitCommitData>();
        var lines = gitLogOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var commit = ParseGitLogLine(line);
                if (commit != null)
                {
                    commits.Add(commit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse git log line: {Line}", line);
            }
        }

        _logger.LogDebug("Parsed {CommitCount} commits from git log output", commits.Count);
        return commits;
    }

    public string? ParseInitialCommitSha(string gitRevListOutput)
    {
        if (string.IsNullOrWhiteSpace(gitRevListOutput))
        {
            _logger.LogDebug("Git rev-list output is empty or whitespace");
            return null;
        }

        var lines = gitRevListOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.FirstOrDefault()?.Trim();

        if (string.IsNullOrEmpty(firstLine))
        {
            _logger.LogDebug("No initial commit found in git rev-list output");
            return null;
        }

        // Validate that it looks like a SHA hash (40 characters, hexadecimal)
        if (firstLine.Length == 40 && firstLine.All(c => char.IsAsciiHexDigit(c)))
        {
            _logger.LogDebug("Found initial commit SHA: {Sha}", firstLine);
            return firstLine;
        }

        _logger.LogWarning("Invalid initial commit SHA format: {FirstLine}", firstLine);
        return null;
    }

    private GitCommitData? ParseGitLogLine(string line)
    {
        // Expected format: "%H|||%an|||%ae|||%aI|||%s"
        // SHA|||AuthorName|||AuthorEmail|||ISO8601Timestamp|||Subject
        
        var parts = line.Split(LogDelimiter, 5); // Split into max 5 parts
        
        if (parts.Length < 5)
        {
            _logger.LogWarning("Git log line has incorrect number of parts ({Count}): {Line}", parts.Length, line);
            return null;
        }

        var sha = parts[0]?.Trim();
        var authorName = parts[1]?.Trim();
        var authorEmail = parts[2]?.Trim();
        var timestampStr = parts[3]?.Trim();
        var message = parts[4]?.Trim();

        if (string.IsNullOrEmpty(sha) || string.IsNullOrEmpty(authorEmail) || string.IsNullOrEmpty(timestampStr))
        {
            _logger.LogWarning("Git log line has empty required fields: {Line}", line);
            return null;
        }

        // Validate SHA format
        if (sha.Length != 40 || !sha.All(c => char.IsAsciiHexDigit(c)))
        {
            _logger.LogWarning("Invalid SHA format in git log line: {Sha}", sha);
            return null;
        }

        // Parse timestamp (ISO 8601 format)
        if (!DateTime.TryParse(timestampStr, null, DateTimeStyles.RoundtripKind, out var timestamp))
        {
            _logger.LogWarning("Invalid timestamp format in git log line: {Timestamp}", timestampStr);
            return null;
        }

        return new GitCommitData
        {
            Sha = sha,
            AuthorName = authorName ?? string.Empty,
            AuthorEmail = authorEmail,
            AuthorTimestamp = timestamp,
            Message = message ?? string.Empty
        };
    }
}