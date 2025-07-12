namespace CodeInventory.Common.Configuration;

public class CrawlSettings
{
    public List<string> RootDirectories { get; set; } = new();
    public List<string> RemoteUrls { get; set; } = new();
}