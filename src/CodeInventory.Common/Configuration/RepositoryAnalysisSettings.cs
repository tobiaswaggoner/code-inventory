namespace CodeInventory.Common.Configuration;

public class RepositoryAnalysisSettings
{
    public bool EnableAnalysis { get; set; } = false;
    public bool SaveDebugFiles { get; set; } = true;
    public string DebugOutputDirectory { get; set; } = string.Empty;
    public bool AnalyzeAfterCrawling { get; set; } = false;
    public int DelayBetweenAnalysesMs { get; set; } = 1000;
}