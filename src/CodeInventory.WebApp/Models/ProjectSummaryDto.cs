namespace CodeInventory.WebApp.Models;

public class ProjectSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Description { get; set; }
    public bool HasHeroImage { get; set; }
    public DateTime? AnalysisDate { get; set; }
    public int CommitCount { get; set; }
    public int LocationCount { get; set; }
}