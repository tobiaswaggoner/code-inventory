using Microsoft.Extensions.Options;
using CodeInventory.Common.Configuration;
using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class DirectoryCrawlerService : BackgroundService
{
    private readonly ILogger<DirectoryCrawlerService> _logger;
    private readonly CrawlSettings _crawlSettings;
    private readonly ICrawlTriggerService _triggerService;
    private readonly IDelayProvider _delayProvider;

    public DirectoryCrawlerService(
        ILogger<DirectoryCrawlerService> logger,
        IOptions<CrawlSettings> crawlSettings,
        ICrawlTriggerService triggerService,
        IDelayProvider delayProvider)
    {
        _logger = logger;
        _crawlSettings = crawlSettings.Value;
        _triggerService = triggerService;
        _delayProvider = delayProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DirectoryCrawlerService: Background service started, waiting for triggers...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for trigger
                var triggered = await _triggerService.WaitForTriggerAsync(stoppingToken);
                
                if (triggered && !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("DirectoryCrawlerService: Crawl triggered, starting execution...");
                    await StartCrawling(stoppingToken);
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
                    await _delayProvider.DelayAsync(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("DirectoryCrawlerService: Service stopping during error recovery delay...");
                    break;
                }
            }
        }
    }

    private async Task StartCrawling(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DirectoryCrawlerService: Crawling {DirectoryCount} root directories and {UrlCount} remote URLs",
            _crawlSettings.RootDirectories.Count,
            _crawlSettings.RemoteUrls.Count);

        // Log configured directories for verification
        foreach (var directory in _crawlSettings.RootDirectories)
        {
            _logger.LogInformation("DirectoryCrawlerService: Configured root directory: {Directory}", directory);
        }

        foreach (var url in _crawlSettings.RemoteUrls)
        {
            _logger.LogInformation("DirectoryCrawlerService: Configured remote URL: {Url}", url);
        }

        // TODO: Implement actual crawling logic in next step
        _logger.LogInformation("DirectoryCrawlerService: Crawling completed successfully");
        
        // Simulate some work for now
        await _delayProvider.DelayAsync(1000, stoppingToken);
    }
}