using CodeInventory.Common.Services;

namespace CodeInventory.Common.Services;

public interface IGitIntegrationService
{
    Task<GitRepositoryData?> ExtractProjectDataAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<GitCommitData>> GetCommitHistoryAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<bool> CheckUncommittedChangesAsync(string repositoryPath, CancellationToken cancellationToken = default);
}

public class GitRepositoryData
{
    public string Path { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? InitialCommitSha { get; set; }
    public bool HasUncommittedChanges { get; set; }
    public IEnumerable<GitCommitData> Commits { get; set; } = Enumerable.Empty<GitCommitData>();
}