using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class RepositoryScanner : IRepositoryScanner
{
    private readonly ILogger<RepositoryScanner> _logger;
    private readonly IGitCommandService _gitCommandService;

    public RepositoryScanner(ILogger<RepositoryScanner> logger, IGitCommandService gitCommandService)
    {
        _logger = logger;
        _gitCommandService = gitCommandService;
    }

    public async Task<IEnumerable<string>> ScanDirectoryForRepositoriesAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rootPath))
        {
            _logger.LogWarning("Root path is null or empty");
            return Enumerable.Empty<string>();
        }

        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Root path does not exist: {RootPath}", rootPath);
            return Enumerable.Empty<string>();
        }

        var repositories = new List<string>();
        
        try
        {
            _logger.LogInformation("Scanning directory for Git repositories: {RootPath}", rootPath);
            await ScanDirectoryRecursiveAsync(rootPath, repositories, cancellationToken);
            _logger.LogInformation("Found {RepositoryCount} Git repositories in {RootPath}", repositories.Count, rootPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory for repositories: {RootPath}", rootPath);
        }

        return repositories;
    }

    public async Task<IEnumerable<string>> GetGitRepositoryPathsAsync(IEnumerable<string> rootDirectories, CancellationToken cancellationToken = default)
    {
        if (rootDirectories == null)
        {
            _logger.LogWarning("Root directories collection is null");
            return Enumerable.Empty<string>();
        }

        var allRepositories = new List<string>();

        foreach (var rootDir in rootDirectories)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var repositories = await ScanDirectoryForRepositoriesAsync(rootDir, cancellationToken);
            allRepositories.AddRange(repositories);
        }

        // Remove duplicates (in case the same repository is found in multiple root directories)
        var uniqueRepositories = allRepositories.Distinct().ToList();
        
        _logger.LogInformation("Found {TotalRepositories} unique Git repositories across {RootDirectoryCount} root directories", 
            uniqueRepositories.Count, rootDirectories.Count());

        return uniqueRepositories;
    }

    private async Task ScanDirectoryRecursiveAsync(string currentPath, List<string> repositories, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            // Check if current directory is a Git repository
            if (await _gitCommandService.IsGitRepositoryAsync(currentPath, cancellationToken))
            {
                _logger.LogDebug("Found Git repository: {Path}", currentPath);
                repositories.Add(currentPath);
                // Don't scan subdirectories of a Git repository to avoid finding nested repositories
                return;
            }

            // Check if .git directory exists (alternative check)
            var gitDir = Path.Combine(currentPath, ".git");
            if (Directory.Exists(gitDir))
            {
                _logger.LogDebug("Found .git directory: {Path}", currentPath);
                repositories.Add(currentPath);
                return;
            }

            // Scan subdirectories
            var subdirectories = Directory.GetDirectories(currentPath);
            foreach (var subdirectory in subdirectories)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Skip common directories that shouldn't contain Git repositories
                var directoryName = Path.GetFileName(subdirectory);
                if (ShouldSkipDirectory(directoryName))
                {
                    _logger.LogTrace("Skipping directory: {Directory}", subdirectory);
                    continue;
                }

                try
                {
                    await ScanDirectoryRecursiveAsync(subdirectory, repositories, cancellationToken);
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogTrace("Access denied to directory: {Directory}", subdirectory);
                }
                catch (DirectoryNotFoundException)
                {
                    _logger.LogTrace("Directory not found (may have been deleted): {Directory}", subdirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error scanning subdirectory: {Directory}", subdirectory);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogTrace("Access denied to directory: {Directory}", currentPath);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogTrace("Directory not found (may have been deleted): {Directory}", currentPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning directory: {Directory}", currentPath);
        }
    }

    private static bool ShouldSkipDirectory(string directoryName)
    {
        // Skip common directories that typically don't contain Git repositories
        var skipDirectories = new[]
        {
            "node_modules",
            "bin",
            "obj",
            ".vs",
            ".vscode",
            ".idea",
            "target",
            "dist",
            "build",
            ".gradle",
            ".nuget",
            "packages",
            "__pycache__",
            ".pytest_cache",
            "venv",
            "env",
            ".env",
            "Temp",
            "tmp",
            "temp",
            ".tmp",
            "cache",
            ".cache",
            "logs",
            "log"
        };

        return skipDirectories.Contains(directoryName, StringComparer.OrdinalIgnoreCase);
    }
}