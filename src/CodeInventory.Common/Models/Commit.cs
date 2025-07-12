namespace CodeInventory.Common.Models;

public class Commit
{
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset AuthorTimestamp { get; set; }
    public Guid AuthorId { get; set; }
    public Guid ProjectId { get; set; }
    
    public Author Author { get; set; } = null!;
    public Project Project { get; set; } = null!;
}