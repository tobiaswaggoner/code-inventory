namespace CodeInventory.Common.Services;

public interface IRepositoryAnalysisService
{
    Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<AnalysisSummary> AnalyzeAllRepositoriesAsync(CancellationToken cancellationToken = default);
}

public class RepositoryAnalysisResult
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; } = string.Empty;
    public string RepositoryPath { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    
    // Analysis Results
    public string? Headline { get; set; }
    public string? Description { get; set; }
    public byte[]? HeroImage { get; set; }
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    
    // Debug/Intermediate Data
    public string? RepomixOutput { get; set; }
    public string? ImagePrompt { get; set; }
    public int TokensUsed { get; set; }
    
    // File Outputs (for debugging)
    public string? DescriptionFilePath { get; set; }
    public string? HeadlineFilePath { get; set; }
    public string? ImagePromptFilePath { get; set; }
    public string? HeroImageFilePath { get; set; }
}

public class AnalysisSummary
{
    public int TotalRepositories { get; set; }
    public int SuccessfulAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    public int TotalTokensUsed { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<string> Errors { get; set; } = new();
}