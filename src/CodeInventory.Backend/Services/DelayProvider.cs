using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class DelayProvider : IDelayProvider
{
    public async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        await Task.Delay(millisecondsDelay, cancellationToken);
    }
}