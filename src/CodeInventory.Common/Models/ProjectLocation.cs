namespace CodeInventory.Common.Models;

public class ProjectLocation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool HasUncommittedChanges { get; set; }
    
    public Project Project { get; set; } = null!;
}