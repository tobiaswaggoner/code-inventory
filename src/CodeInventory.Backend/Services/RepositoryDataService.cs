using CodeInventory.Backend.Data;
using CodeInventory.Common.Models;
using CodeInventory.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace CodeInventory.Backend.Services;

public class RepositoryDataService : IRepositoryDataService
{
    private readonly ILogger<RepositoryDataService> _logger;
    private readonly ApplicationDbContext _context;

    public RepositoryDataService(ILogger<RepositoryDataService> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task SaveRepositoryDataAsync(GitRepositoryData repositoryData, CancellationToken cancellationToken = default)
    {
        if (repositoryData == null)
        {
            throw new ArgumentNullException(nameof(repositoryData));
        }

        if (string.IsNullOrEmpty(repositoryData.InitialCommitSha))
        {
            _logger.LogWarning("Cannot save repository data without initial commit SHA for path: {Path}", repositoryData.Path);
            return;
        }

        try
        {
            _logger.LogDebug("Saving repository data for {ProjectName} at {Path}", repositoryData.ProjectName, repositoryData.Path);

            // Find or create project using initial commit SHA for deduplication
            var project = await GetOrCreateProjectAsync(repositoryData.ProjectName, repositoryData.InitialCommitSha, cancellationToken);

            // Add or update project location
            await AddOrUpdateProjectLocationAsync(project.Id, repositoryData.Path, repositoryData.HasUncommittedChanges, cancellationToken);

            // Process commits
            await ProcessCommitsAsync(project.Id, repositoryData.Commits, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully saved repository data for {ProjectName} with {CommitCount} commits", 
                repositoryData.ProjectName, repositoryData.Commits.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving repository data for {ProjectName} at {Path}", repositoryData.ProjectName, repositoryData.Path);
            throw;
        }
    }

    private async Task<Project> GetOrCreateProjectAsync(string projectName, string initialCommitSha, CancellationToken cancellationToken)
    {
        // Try to find existing project by initial commit SHA
        var existingProject = await _context.Projects
            .FirstOrDefaultAsync(p => p.InitialCommitSha == initialCommitSha, cancellationToken);

        if (existingProject != null)
        {
            _logger.LogDebug("Found existing project {ProjectName} with initial commit SHA: {Sha}", existingProject.Name, initialCommitSha);
            return existingProject;
        }

        // Create new project
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = projectName,
            InitialCommitSha = initialCommitSha
        };

        _context.Projects.Add(project);
        _logger.LogDebug("Created new project {ProjectName} with initial commit SHA: {Sha}", projectName, initialCommitSha);

        return project;
    }

    private async Task AddOrUpdateProjectLocationAsync(Guid projectId, string path, bool hasUncommittedChanges, CancellationToken cancellationToken)
    {
        // Try to find existing project location
        var existingLocation = await _context.ProjectLocations
            .FirstOrDefaultAsync(pl => pl.ProjectId == projectId && pl.Path == path, cancellationToken);

        if (existingLocation != null)
        {
            // Update existing location
            existingLocation.HasUncommittedChanges = hasUncommittedChanges;
            _logger.LogDebug("Updated existing project location: {Path}", path);
        }
        else
        {
            // Create new project location
            var projectLocation = new ProjectLocation
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Location = Path.GetDirectoryName(path) ?? string.Empty,
                Path = path,
                HasUncommittedChanges = hasUncommittedChanges
            };

            _context.ProjectLocations.Add(projectLocation);
            _logger.LogDebug("Created new project location: {Path}", path);
        }
    }

    private async Task ProcessCommitsAsync(Guid projectId, IEnumerable<GitCommitData> commits, CancellationToken cancellationToken)
    {
        foreach (var commitData in commits)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Check if commit already exists (global deduplication by SHA)
            var existingCommit = await _context.Commits
                .FirstOrDefaultAsync(c => c.Sha == commitData.Sha, cancellationToken);

            if (existingCommit != null)
            {
                _logger.LogTrace("Commit {Sha} already exists, skipping", commitData.Sha);
                continue;
            }

            // Get or create author
            var author = await GetOrCreateAuthorAsync(commitData.AuthorName, commitData.AuthorEmail, cancellationToken);

            // Create new commit
            var commit = new Commit
            {
                Sha = commitData.Sha,
                Message = commitData.Message,
                AuthorTimestamp = commitData.AuthorTimestamp,
                AuthorId = author.Id,
                ProjectId = projectId
            };

            _context.Commits.Add(commit);
            _logger.LogTrace("Added commit {Sha} for project {ProjectId}", commitData.Sha, projectId);
        }
    }

    private async Task<Author> GetOrCreateAuthorAsync(string authorName, string authorEmail, CancellationToken cancellationToken)
    {
        // Try to find existing author by email (deduplication by email)
        var existingAuthor = await _context.Authors
            .FirstOrDefaultAsync(a => a.Email == authorEmail, cancellationToken);

        if (existingAuthor != null)
        {
            // Update name if it's different (in case the author changed their name)
            if (existingAuthor.Name != authorName && !string.IsNullOrEmpty(authorName))
            {
                existingAuthor.Name = authorName;
                _logger.LogTrace("Updated author name for {Email}: {Name}", authorEmail, authorName);
            }
            return existingAuthor;
        }

        // Create new author
        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = authorName,
            Email = authorEmail
        };

        _context.Authors.Add(author);
        _logger.LogTrace("Created new author {Name} ({Email})", authorName, authorEmail);

        return author;
    }

    public async Task<int> GetRepositoryCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Projects.CountAsync(cancellationToken);
    }

    public async Task<int> GetCommitCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Commits.CountAsync(cancellationToken);
    }

    public async Task<int> GetAuthorCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Authors.CountAsync(cancellationToken);
    }
}