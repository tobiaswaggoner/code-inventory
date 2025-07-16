using Microsoft.Extensions.Options;
using CodeInventory.Common.Configuration;
using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class DirectoryCrawlerService : BackgroundService
{
    private readonly ILogger<DirectoryCrawlerService> _logger;
    private readonly CrawlSettings _crawlSettings;
    private readonly ICrawlTriggerService _triggerService;
    private readonly IServiceScopeFactory _scopeFactory;

    public DirectoryCrawlerService(
        ILogger<DirectoryCrawlerService> logger,
        IOptions<CrawlSettings> crawlSettings,
        ICrawlTriggerService triggerService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _crawlSettings = crawlSettings.Value;
        _triggerService = triggerService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DirectoryCrawlerService: Background service started, waiting for triggers...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                
                // Wait for trigger
                var triggered = await _triggerService.WaitForTriggerAsync(stoppingToken);
                
                if (triggered && !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("DirectoryCrawlerService: Crawl triggered, starting execution...");
                    await StartCrawling(stoppingToken, scope);
                    _triggerService.CompleteCrawl();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DirectoryCrawlerService: Service stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DirectoryCrawlerService: Error during crawling execution");
                _triggerService.CompleteCrawl();
                
                // Wait a bit before next trigger check
                try
                {
                    var delayProvider = scope.ServiceProvider.GetRequiredService<IDelayProvider>();
                    await delayProvider.DelayAsync(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("DirectoryCrawlerService: Service stopping during error recovery delay...");
                    break;
                }
            }
        }
    }

    private async Task StartCrawling(CancellationToken stoppingToken, IServiceScope scope)
    {
        _logger.LogInformation("DirectoryCrawlerService: Starting crawl of {DirectoryCount} root directories and {UrlCount} remote URLs",
            _crawlSettings.RootDirectories.Count,
            _crawlSettings.RemoteUrls.Count);

        var totalRepositories = 0;
        var totalCommits = 0;
        var totalErrors = 0;

        try
        {
            // Step 1: Scan local directories for Git repositories
            if (_crawlSettings.RootDirectories.Count > 0)
            {
                _logger.LogInformation("DirectoryCrawlerService: Scanning {DirectoryCount} root directories for Git repositories", _crawlSettings.RootDirectories.Count);
                
                var repositoryScanner = scope.ServiceProvider.GetRequiredService<IRepositoryScanner>();
                var repositoryPaths = (await repositoryScanner.GetGitRepositoryPathsAsync(_crawlSettings.RootDirectories, stoppingToken))
                    .ToList();
                totalRepositories = repositoryPaths.Count;
                
                _logger.LogInformation("DirectoryCrawlerService: Found {RepositoryCount} Git repositories", totalRepositories);

                // Step 2: Process each repository
                foreach (var repositoryPath in repositoryPaths)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("DirectoryCrawlerService: Crawl cancelled, stopping repository processing");
                        break;
                    }

                    try
                    {
                        _logger.LogDebug("DirectoryCrawlerService: Processing repository: {Path}", repositoryPath);

                        // Extract Git data from repository
                        var gitIntegrationService = scope.ServiceProvider.GetRequiredService<IGitIntegrationService>();
                        var repositoryData = await gitIntegrationService.ExtractProjectDataAsync(repositoryPath, stoppingToken);
                        
                        if (repositoryData != null)
                        {
                            // Save to database
                            var repositoryDataService = scope.ServiceProvider.GetRequiredService<IRepositoryDataService>();
                            await repositoryDataService.SaveRepositoryDataAsync(repositoryData, stoppingToken);
                            
                            totalCommits += repositoryData.Commits.Count();
                            _logger.LogInformation("DirectoryCrawlerService: Successfully processed {ProjectName} with {CommitCount} commits", 
                                repositoryData.ProjectName, repositoryData.Commits.Count());
                        }
                        else
                        {
                            _logger.LogWarning("DirectoryCrawlerService: Failed to extract data from repository: {Path}", repositoryPath);
                            totalErrors++;
                        }

                        // Small delay between repositories to avoid overwhelming the system
                        var delayProvider = scope.ServiceProvider.GetRequiredService<IDelayProvider>();
                        await delayProvider.DelayAsync(100, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "DirectoryCrawlerService: Error processing repository: {Path}", repositoryPath);
                        totalErrors++;
                    }
                }
            }

            // Step 3: Process remote URLs (TODO: Implement in future phase)
            if (_crawlSettings.RemoteUrls.Count > 0)
            {
                _logger.LogInformation("DirectoryCrawlerService: Remote URL processing not yet implemented. {UrlCount} URLs will be skipped.", 
                    _crawlSettings.RemoteUrls.Count);
                
                foreach (var url in _crawlSettings.RemoteUrls)
                {
                    _logger.LogDebug("DirectoryCrawlerService: Skipping remote URL: {Url}", url);
                }
            }

            // Step 4: Log final statistics
            var finalStats = await GetCrawlStatisticsAsync(stoppingToken, scope);
            
            _logger.LogInformation("DirectoryCrawlerService: Crawl completed successfully. " +
                                 "Processed {ProcessedRepos} repositories, {ProcessedCommits} commits, {Errors} errors. " +
                                 "Total in database: {TotalProjects} projects, {TotalCommits} commits, {TotalAuthors} authors",
                totalRepositories, totalCommits, totalErrors,
                finalStats.TotalProjects, finalStats.TotalCommits, finalStats.TotalAuthors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DirectoryCrawlerService: Fatal error during crawling");
            throw;
        }
    }

    private async Task<CrawlStatistics> GetCrawlStatisticsAsync(CancellationToken stoppingToken, IServiceScope scope)
    {
        try
        {
            var repositoryDataService = scope.ServiceProvider.GetRequiredService<IRepositoryDataService>();
            var projectCount = await repositoryDataService.GetRepositoryCountAsync(stoppingToken);
            var commitCount = await repositoryDataService.GetCommitCountAsync(stoppingToken);
            var authorCount = await repositoryDataService.GetAuthorCountAsync(stoppingToken);

            return new CrawlStatistics
            {
                TotalProjects = projectCount,
                TotalCommits = commitCount,
                TotalAuthors = authorCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DirectoryCrawlerService: Error getting crawl statistics");
            return new CrawlStatistics();
        }
    }

    private class CrawlStatistics
    {
        public int TotalProjects { get; set; }
        public int TotalCommits { get; set; }
        public int TotalAuthors { get; set; }
    }
}