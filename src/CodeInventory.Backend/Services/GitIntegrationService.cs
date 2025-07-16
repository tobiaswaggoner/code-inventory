using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class GitIntegrationService : IGitIntegrationService
{
    private readonly ILogger<GitIntegrationService> _logger;
    private readonly IGitCommandService _gitCommandService;
    private readonly IGitLogParser _gitLogParser;

    public GitIntegrationService(
        ILogger<GitIntegrationService> logger,
        IGitCommandService gitCommandService,
        IGitLogParser gitLogParser)
    {
        _logger = logger;
        _gitCommandService = gitCommandService;
        _gitLogParser = gitLogParser;
    }

    public async Task<GitRepositoryData?> ExtractProjectDataAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            _logger.LogWarning("Repository path is null or empty");
            return null;
        }

        if (!Directory.Exists(repositoryPath))
        {
            _logger.LogWarning("Repository path does not exist: {Path}", repositoryPath);
            return null;
        }

        try
        {
            _logger.LogDebug("Extracting project data from repository: {Path}", repositoryPath);

            // Verify it's a Git repository
            if (!await _gitCommandService.IsGitRepositoryAsync(repositoryPath, cancellationToken))
            {
                _logger.LogWarning("Path is not a Git repository: {Path}", repositoryPath);
                return null;
            }

            // Get project name (directory name)
            var projectName = Path.GetFileName(repositoryPath);
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = "Unknown";
            }

            // Get initial commit SHA
            var initialCommitSha = await GetInitialCommitShaAsync(repositoryPath, cancellationToken);
            if (string.IsNullOrEmpty(initialCommitSha))
            {
                _logger.LogWarning("Could not determine initial commit SHA for repository: {Path}", repositoryPath);
                return null;
            }

            // Get commit history
            var commits = await GetCommitHistoryAsync(repositoryPath, cancellationToken);

            // Check for uncommitted changes
            var hasUncommittedChanges = await CheckUncommittedChangesAsync(repositoryPath, cancellationToken);

            var repositoryData = new GitRepositoryData
            {
                Path = repositoryPath,
                ProjectName = projectName,
                InitialCommitSha = initialCommitSha,
                HasUncommittedChanges = hasUncommittedChanges,
                Commits = commits
            };

            _logger.LogInformation("Successfully extracted project data for {ProjectName} with {CommitCount} commits", 
                projectName, commits.Count());

            return repositoryData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting project data from repository: {Path}", repositoryPath);
            return null;
        }
    }

    public async Task<IEnumerable<GitCommitData>> GetCommitHistoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            _logger.LogWarning("Repository path is null or empty");
            return Enumerable.Empty<GitCommitData>();
        }

        try
        {
            _logger.LogDebug("Getting commit history for repository: {Path}", repositoryPath);

            // Execute git log command with format specified in CLAUDE.md
            var result = await _gitCommandService.ExecuteGitCommandAsync(
                repositoryPath,
                "log --all --pretty=format:\"%H|||%an|||%ae|||%aI|||%s\" --no-patch",
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Git log command failed for repository {Path}: {Error}", repositoryPath, result.Error);
                return Enumerable.Empty<GitCommitData>();
            }

            // Parse the git log output
            var commits = _gitLogParser.ParseGitLogOutput(result.Output);
            
            _logger.LogDebug("Retrieved {CommitCount} commits from repository: {Path}", commits.Count(), repositoryPath);
            
            return commits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commit history for repository: {Path}", repositoryPath);
            return Enumerable.Empty<GitCommitData>();
        }
    }

    public async Task<bool> CheckUncommittedChangesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            _logger.LogWarning("Repository path is null or empty");
            return false;
        }

        try
        {
            _logger.LogDebug("Checking for uncommitted changes in repository: {Path}", repositoryPath);

            // Execute git status command with porcelain format
            var result = await _gitCommandService.ExecuteGitCommandAsync(
                repositoryPath,
                "status --porcelain",
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Git status command failed for repository {Path}: {Error}", repositoryPath, result.Error);
                return false;
            }

            // If output is empty, there are no uncommitted changes
            var hasChanges = !string.IsNullOrWhiteSpace(result.Output);
            
            _logger.LogDebug("Repository {Path} has uncommitted changes: {HasChanges}", repositoryPath, hasChanges);
            
            return hasChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking uncommitted changes for repository: {Path}", repositoryPath);
            return false;
        }
    }

    private async Task<string?> GetInitialCommitShaAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting initial commit SHA for repository: {Path}", repositoryPath);

            // Execute git rev-list command to get the initial commit
            var result = await _gitCommandService.ExecuteGitCommandAsync(
                repositoryPath,
                "rev-list --max-parents=0 HEAD",
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Git rev-list command failed for repository {Path}: {Error}", repositoryPath, result.Error);
                return null;
            }

            // Parse the result to get the initial commit SHA
            var initialCommitSha = _gitLogParser.ParseInitialCommitSha(result.Output);
            
            if (string.IsNullOrEmpty(initialCommitSha))
            {
                _logger.LogWarning("Could not parse initial commit SHA for repository: {Path}", repositoryPath);
                return null;
            }

            _logger.LogDebug("Found initial commit SHA {Sha} for repository: {Path}", initialCommitSha, repositoryPath);
            
            return initialCommitSha;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting initial commit SHA for repository: {Path}", repositoryPath);
            return null;
        }
    }
}