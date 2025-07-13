using CodeInventory.Common.Services;
using CodeInventory.Common.Models;

namespace CodeInventory.Common.Services;

public interface IRepositoryDataService
{
    Task SaveRepositoryDataAsync(GitRepositoryData repositoryData, CancellationToken cancellationToken = default);
    Task<int> GetRepositoryCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetCommitCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetAuthorCountAsync(CancellationToken cancellationToken = default);
    
    // Repository Analysis Methods
    Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task UpdateProjectAnalysisAsync(Guid projectId, string? headline, string? description, byte[]? heroImage, DateTime analysisDate, string? repomixOutput = null, CancellationToken cancellationToken = default);
}