using CodeInventory.Common.Services;

namespace CodeInventory.Common.Services;

public interface IRepositoryDataService
{
    Task SaveRepositoryDataAsync(GitRepositoryData repositoryData, CancellationToken cancellationToken = default);
    Task<int> GetRepositoryCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetCommitCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetAuthorCountAsync(CancellationToken cancellationToken = default);
}