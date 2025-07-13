namespace CodeInventory.Common.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InitialCommitSha { get; set; } = string.Empty;
    
    // Repository Analysis Data
    public string? Headline { get; set; }
    public string? Description { get; set; }
    public byte[]? HeroImage { get; set; }
    public DateTime? AnalysisDate { get; set; }
    public string? RepomixOutput { get; set; }
    
    public ICollection<ProjectLocation> Locations { get; set; } = new List<ProjectLocation>();
    public ICollection<Commit> Commits { get; set; } = new List<Commit>();
}