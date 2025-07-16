using NUnit.Framework;
using CodeInventory.Backend.Services;
using System.Diagnostics;

namespace CodeInventory.Backend.Tests.Services;

[TestFixture]
public class DelayProviderTests
{
    private DelayProvider _delayProvider;

    [SetUp]
    public void Setup()
    {
        _delayProvider = new DelayProvider();
    }

    [Test]
    public async Task DelayAsync_DelaysForSpecifiedDuration()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        const int delayMs = 100;

        // Act
        await _delayProvider.DelayAsync(delayMs);

        // Assert
        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(delayMs - 10)); // Allow some tolerance
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(delayMs + 50)); // But not too much
    }

    [Test]
    public async Task DelayAsync_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();

        // Act & Assert
        var delayTask = _delayProvider.DelayAsync(1000, cts.Token);
        
        // Cancel after 50ms
        await Task.Delay(50);
        cts.Cancel();
        
        try
        {
            await delayTask;
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500)); // Should be cancelled quickly
            Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token));
        }
    }

    [Test]
    public async Task DelayAsync_WithZeroDelay_CompletesImmediately()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _delayProvider.DelayAsync(0);

        // Assert
        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(50)); // Should complete very quickly
    }
}