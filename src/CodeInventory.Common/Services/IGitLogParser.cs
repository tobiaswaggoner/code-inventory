using CodeInventory.Common.Models;

namespace CodeInventory.Common.Services;

public interface IGitLogParser
{
    IEnumerable<GitCommitData> ParseGitLogOutput(string gitLogOutput);
    string? ParseInitialCommitSha(string gitRevListOutput);
}

public class GitCommitData
{
    public string Sha { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTime AuthorTimestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}