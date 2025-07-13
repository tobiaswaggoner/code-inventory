using CodeInventory.WebApp.Models;

namespace CodeInventory.WebApp.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<byte[]?> GetProjectImageAsync(Guid projectId, CancellationToken cancellationToken = default);
}