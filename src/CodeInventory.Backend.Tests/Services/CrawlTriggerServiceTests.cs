using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using CodeInventory.Backend.Services;

namespace CodeInventory.Backend.Tests.Services;

[TestFixture]
public class CrawlTriggerServiceTests
{
    private ILogger<CrawlTriggerService> _logger;
    private CrawlTriggerService _service;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<CrawlTriggerService>>();
        _service = new CrawlTriggerService(_logger);
    }

    [Test]
    public void IsCrawling_InitiallyFalse()
    {
        // Assert
        Assert.That(_service.IsCrawling, Is.False);
    }

    [Test]
    public async Task TriggerCrawlAsync_SetsIsCrawlingToTrue()
    {
        // Act
        await _service.TriggerCrawlAsync();

        // Assert
        Assert.That(_service.IsCrawling, Is.True);
    }

    [Test]
    public async Task TriggerCrawlAsync_WhenAlreadyCrawling_DoesNotTriggerAgain()
    {
        // Arrange
        await _service.TriggerCrawlAsync();
        var wasCrawling = _service.IsCrawling;

        // Act
        await _service.TriggerCrawlAsync();

        // Assert
        Assert.That(wasCrawling, Is.True);
        Assert.That(_service.IsCrawling, Is.True);
    }

    [Test]
    public void CompleteCrawl_SetsIsCrawlingToFalse()
    {
        // Arrange
        _service.TriggerCrawlAsync().Wait();
        var wasCrawling = _service.IsCrawling;

        // Act
        _service.CompleteCrawl();

        // Assert
        Assert.That(wasCrawling, Is.True);
        Assert.That(_service.IsCrawling, Is.False);
    }

    [Test]
    public async Task WaitForTriggerAsync_WhenNotTriggered_WaitsForTrigger()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var waitTask = _service.WaitForTriggerAsync(cts.Token);

        // Act - trigger after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await _service.TriggerCrawlAsync();
        });

        var result = await waitTask;

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_service.IsCrawling, Is.True);
    }

    [Test]
    public async Task WaitForTriggerAsync_WhenCancelled_ReturnsFalse()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var waitTask = _service.WaitForTriggerAsync(cts.Token);

        // Act - cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            cts.Cancel();
        });

        var result = await waitTask;

        // Assert
        Assert.That(result, Is.False);
    }
}