namespace CodeInventory.Common.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InitialCommitSha { get; set; } = string.Empty;
    
    public ICollection<ProjectLocation> Locations { get; set; } = new List<ProjectLocation>();
    public ICollection<Commit> Commits { get; set; } = new List<Commit>();
}