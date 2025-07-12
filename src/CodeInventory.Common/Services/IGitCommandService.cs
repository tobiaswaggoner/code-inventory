namespace CodeInventory.Common.Services;

public interface IGitCommandService
{
    Task<GitCommandResult> ExecuteGitCommandAsync(string workingDirectory, string arguments, CancellationToken cancellationToken = default);
    Task<bool> IsGitRepositoryAsync(string path, CancellationToken cancellationToken = default);
}

public class GitCommandResult
{
    public bool IsSuccess { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}