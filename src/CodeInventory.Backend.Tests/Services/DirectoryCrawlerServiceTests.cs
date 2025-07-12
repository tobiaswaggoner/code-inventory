using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using CodeInventory.Backend.Services;
using CodeInventory.Common.Configuration;
using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Tests.Services;

[TestFixture]
public class DirectoryCrawlerServiceTests
{
    private ILogger<DirectoryCrawlerService> _logger;
    private IOptions<CrawlSettings> _crawlSettings;
    private ICrawlTriggerService _triggerService;
    private IDelayProvider _delayProvider;
    private IRepositoryScanner _repositoryScanner;
    private IGitIntegrationService _gitIntegrationService;
    private IRepositoryDataService _repositoryDataService;
    private DirectoryCrawlerService _service;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<DirectoryCrawlerService>>();
        _delayProvider = Substitute.For<IDelayProvider>();
        _repositoryScanner = Substitute.For<IRepositoryScanner>();
        _gitIntegrationService = Substitute.For<IGitIntegrationService>();
        _repositoryDataService = Substitute.For<IRepositoryDataService>();
        
        var crawlSettings = new CrawlSettings
        {
            RootDirectories = new List<string> { "/test/dir1", "/test/dir2" },
            RemoteUrls = new List<string> { "https://github.com/test/repo.git" }
        };
        _crawlSettings = Options.Create(crawlSettings);
        
        _triggerService = Substitute.For<ICrawlTriggerService>();
        
        _service = new DirectoryCrawlerService(_logger, _crawlSettings, _triggerService, _delayProvider, _repositoryScanner, _gitIntegrationService, _repositoryDataService);
    }

    [Test]
    public void Constructor_InitializesWithCorrectSettings()
    {
        // Arrange & Act - constructor called in Setup
        
        // Assert - verify service was created successfully
        Assert.That(_service, Is.Not.Null);
    }

    [Test]
    public async Task Service_UsesDelayProvider_ForSimulatedWork()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        // Set up repository scanner to return at least one repository
        _repositoryScanner.GetGitRepositoryPathsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "/test/repo1" });
        
        // Set up git integration service to return null (simulating no data extracted)
        _gitIntegrationService.ExtractProjectDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CodeInventory.Common.Services.GitRepositoryData?>(null));
        
        // Set up trigger service to return true once, then cancel on second call
        var triggerCount = 0;
        _triggerService.WaitForTriggerAsync(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var token = callInfo.Arg<CancellationToken>();
            if (triggerCount == 0)
            {
                triggerCount++;
                return Task.FromResult(true);
            }
            
            // Cancel after first trigger is processed
            cts.Cancel();
            return Task.FromCanceled<bool>(token);
        });

        // Act
        await _service.StartAsync(cancellationToken);
        
        // Give ExecuteAsync time to process the trigger
        await Task.Delay(300);
        
        await _service.StopAsync(CancellationToken.None);

        // Assert
        await _delayProvider.Received(1).DelayAsync(100, Arg.Any<CancellationToken>());
        _triggerService.Received(1).CompleteCrawl();
    }

    [Test]
    public async Task Service_HandlesCancellation_Gracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        // Set up trigger service to handle cancellation properly
        _triggerService.WaitForTriggerAsync(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var token = callInfo.Arg<CancellationToken>();
            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(token);
            }
            
            // If not cancelled, return a task that will be cancelled when the token is cancelled
            var tcs = new TaskCompletionSource<bool>();
            token.Register(() => tcs.TrySetCanceled(token));
            return tcs.Task;
        });

        // Act
        await _service.StartAsync(cancellationToken);
        
        // Cancel the token
        cts.Cancel();
        
        // Give ExecuteAsync time to process cancellation
        await Task.Delay(200);
        
        await _service.StopAsync(CancellationToken.None);

        // Assert - Service should handle cancellation gracefully without throwing
        Assert.Pass("Service handled cancellation gracefully");
    }

    [Test]
    public async Task Service_CallsCompleteCrawl_OnException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        // Set up repository scanner to return at least one repository
        _repositoryScanner.GetGitRepositoryPathsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "/test/repo1" });
        
        // Set up git integration service to return null (simulating no data extracted)
        _gitIntegrationService.ExtractProjectDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CodeInventory.Common.Services.GitRepositoryData?>(null));
        
        // Set up trigger service to return true once, then cancel on second call
        var triggerCount = 0;
        _triggerService.WaitForTriggerAsync(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var token = callInfo.Arg<CancellationToken>();
            if (triggerCount == 0)
            {
                triggerCount++;
                return Task.FromResult(true);
            }
            
            // Cancel after first trigger is processed and exception is handled
            cts.Cancel();
            return Task.FromCanceled<bool>(token);
        });
        
        // Make DelayAsync throw an exception for the 100ms delay (between repositories), but succeed for the 5000ms delay (error recovery)
        _delayProvider.DelayAsync(100, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Test exception")));
        _delayProvider.DelayAsync(5000, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(cancellationToken);
        
        // Give ExecuteAsync time to process the trigger and handle the exception
        await Task.Delay(300);
        
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _triggerService.Received(1).CompleteCrawl();
        await _delayProvider.Received(1).DelayAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}