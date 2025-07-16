namespace CodeInventory.Common.Services;

public interface ICrawlTriggerService
{
    Task TriggerCrawlAsync();
    Task<bool> WaitForTriggerAsync(CancellationToken cancellationToken = default);
    void CompleteCrawl();
    bool IsCrawling { get; }
}