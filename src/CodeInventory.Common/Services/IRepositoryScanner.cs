namespace CodeInventory.Common.Services;

public interface IRepositoryScanner
{
    Task<IEnumerable<string>> ScanDirectoryForRepositoriesAsync(string rootPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetGitRepositoryPathsAsync(IEnumerable<string> rootDirectories, CancellationToken cancellationToken = default);
}