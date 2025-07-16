using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class CrawlTriggerService : ICrawlTriggerService
{
    private readonly ILogger<CrawlTriggerService> _logger;
    private readonly SemaphoreSlim _crawlSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _currentCrawlTrigger;

    public CrawlTriggerService(ILogger<CrawlTriggerService> logger)
    {
        _logger = logger;
    }

    public bool IsCrawling { get; private set; }

    public async Task TriggerCrawlAsync()
    {
        if (IsCrawling)
        {
            _logger.LogWarning("CrawlTriggerService: Crawl already in progress, ignoring trigger");
            return;
        }

        await _crawlSemaphore.WaitAsync();
        try
        {
            if (IsCrawling)
            {
                _logger.LogWarning("CrawlTriggerService: Crawl already in progress, ignoring trigger");
                return;
            }

            _logger.LogInformation("CrawlTriggerService: Triggering crawl execution");
            IsCrawling = true;
            
            // Signal waiting background service
            _currentCrawlTrigger?.SetResult(true);
            _currentCrawlTrigger = new TaskCompletionSource<bool>();
        }
        finally
        {
            _crawlSemaphore.Release();
        }
    }

    public async Task<bool> WaitForTriggerAsync(CancellationToken cancellationToken = default)
    {
        _currentCrawlTrigger ??= new TaskCompletionSource<bool>();
        
        using var registration = cancellationToken.Register(() => _currentCrawlTrigger.TrySetCanceled());
        
        try
        {
            return await _currentCrawlTrigger.Task;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void CompleteCrawl()
    {
        _logger.LogInformation("CrawlTriggerService: Crawl completed");
        IsCrawling = false;
    }
}