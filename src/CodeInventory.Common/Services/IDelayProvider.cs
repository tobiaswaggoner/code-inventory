namespace CodeInventory.Common.Services;

public interface IDelayProvider
{
    Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default);
}