namespace CodeInventory.Common.Services;

public interface IRepomixService
{
    Task<RepomixResult> GenerateRepomixOutputAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task CreateRepomixIgnoreFileAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<bool> IsRepomixAvailableAsync();
}

public class RepomixResult
{
    public bool IsSuccess { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string RepomixFilePath { get; set; } = string.Empty;
    public int TokenCount { get; set; }
}