using System.Diagnostics;
using Microsoft.Extensions.Options;
using CodeInventory.Common.Services;
using CodeInventory.Common.Configuration;

namespace CodeInventory.Backend.Services;

public class RepositoryAnalysisService : IRepositoryAnalysisService
{
    private readonly ILogger<RepositoryAnalysisService> _logger;
    private readonly IRepomixService _repomixService;
    private readonly IGeminiApiService _geminiApiService;
    private readonly IRepositoryDataService _repositoryDataService;
    private readonly RepositoryAnalysisSettings _settings;

    public RepositoryAnalysisService(
        ILogger<RepositoryAnalysisService> logger,
        IRepomixService repomixService,
        IGeminiApiService geminiApiService,
        IRepositoryDataService repositoryDataService,
        IOptions<RepositoryAnalysisSettings> settings)
    {
        _logger = logger;
        _repomixService = repomixService;
        _geminiApiService = geminiApiService;
        _repositoryDataService = repositoryDataService;
        _settings = settings.Value;
    }

    public async Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return new RepositoryAnalysisResult
            {
                IsSuccess = false,
                Error = "Repository path cannot be null or empty"
            };
        }

        if (!Directory.Exists(repositoryPath))
        {
            return new RepositoryAnalysisResult
            {
                IsSuccess = false,
                Error = $"Repository directory does not exist: {repositoryPath}",
                RepositoryPath = repositoryPath
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new RepositoryAnalysisResult
        {
            RepositoryPath = repositoryPath,
            AnalysisDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting repository analysis for: {RepositoryPath}", repositoryPath);

            // Step 1: Generate repomix output
            var repomixResult = await _repomixService.GenerateRepomixOutputAsync(repositoryPath, cancellationToken);
            if (!repomixResult.IsSuccess)
            {
                result.IsSuccess = false;
                result.Error = $"Repomix generation failed: {repomixResult.Error}";
                return result;
            }

            result.RepomixOutput = repomixResult.Output;
            _logger.LogInformation("Repomix generation completed. Token count: {TokenCount}", repomixResult.TokenCount);
            
            if(repomixResult.TokenCount > 400000)
            {
                result.IsSuccess = false;
                result.Error = $"Repomix output exceeds maximum token limit of 400.000. Current: {repomixResult.TokenCount}";
                return result;
            }

            // Step 2: Generate project description with Gemini
            var descriptionResult = await _geminiApiService.GenerateProjectDescriptionAsync(repomixResult.Output, cancellationToken);
            if (!descriptionResult.IsSuccess)
            {
                result.IsSuccess = false;
                result.Error = $"Project description generation failed: {descriptionResult.Error}";
                return result;
            }

            result.Description = descriptionResult.Content;
            result.TokensUsed += descriptionResult.TokensUsed;
            _logger.LogDebug("Project description generated. Length: {Length}", descriptionResult.Content.Length);

            // Step 3: Generate one-line headline with Gemini
            var headlineResult = await _geminiApiService.GenerateOneLineDescriptionAsync(descriptionResult.Content, cancellationToken);
            if (!headlineResult.IsSuccess)
            {
                result.IsSuccess = false;
                result.Error = $"Headline generation failed: {headlineResult.Error}";
                return result;
            }

            result.Headline = headlineResult.Content;
            result.TokensUsed += headlineResult.TokensUsed;
            _logger.LogDebug("Headline generated: {Headline}", headlineResult.Content);

            // Step 4: Generate image prompt with Gemini
            var imagePromptResult = await _geminiApiService.GenerateImagePromptAsync(descriptionResult.Content, cancellationToken);
            if (!imagePromptResult.IsSuccess)
            {
                result.IsSuccess = false;
                result.Error = $"Image prompt generation failed: {imagePromptResult.Error}";
                return result;
            }

            result.ImagePrompt = imagePromptResult.Content;
            result.TokensUsed += imagePromptResult.TokensUsed;
            _logger.LogDebug("Image prompt generated. Length: {Length}", imagePromptResult.Content.Length);

            // Step 5: Generate hero image with Gemini
            var imageResult = await _geminiApiService.GenerateImageAsync(imagePromptResult.Content, cancellationToken);
            if (!imageResult.IsSuccess)
            {
                _logger.LogWarning("Hero image generation failed: {Error}. Continuing without image.", imageResult.Error);
                // Don't fail the entire analysis if image generation fails
            }
            else
            {
                result.HeroImage = imageResult.ImageData;
                _logger.LogDebug("Hero image generated. Size: {Size} bytes", imageResult.ImageData.Length);
            }

            // Step 6: Save debug files if enabled
            if (_settings.SaveDebugFiles)
            {
                await SaveDebugFilesAsync(repositoryPath, result, cancellationToken);
            }

            result.IsSuccess = true;
            stopwatch.Stop();
            
            _logger.LogInformation("Repository analysis completed successfully for {RepositoryPath} in {Duration}ms. Tokens used: {TokensUsed}",
                repositoryPath, stopwatch.ElapsedMilliseconds, result.TokensUsed);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during repository analysis for {RepositoryPath}", repositoryPath);
            
            result.IsSuccess = false;
            result.Error = $"Exception occurred: {ex.Message}";
            return result;
        }
    }

    public async Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get project from database to find its path
            var project = await _repositoryDataService.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
            {
                return new RepositoryAnalysisResult
                {
                    IsSuccess = false,
                    Error = $"Project with ID {projectId} not found",
                    ProjectId = projectId
                };
            }

            // Get the first location path
            var primaryLocation = project.Locations.FirstOrDefault();
            if (primaryLocation == null)
            {
                return new RepositoryAnalysisResult
                {
                    IsSuccess = false,
                    Error = $"No locations found for project {project.Name}",
                    ProjectId = projectId
                };
            }

            var result = await AnalyzeRepositoryAsync(primaryLocation.Path, cancellationToken);
            result.ProjectId = projectId;

            // Save results to database if analysis was successful
            if (result.IsSuccess)
            {
                await _repositoryDataService.UpdateProjectAnalysisAsync(
                    projectId,
                    result.Headline,
                    result.Description,
                    result.HeroImage,
                    result.AnalysisDate,
                    result.RepomixOutput,
                    cancellationToken);
                
                _logger.LogInformation("Analysis results saved to database for project {ProjectId}", projectId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing repository for project {ProjectId}", projectId);
            return new RepositoryAnalysisResult
            {
                IsSuccess = false,
                Error = $"Exception occurred: {ex.Message}",
                ProjectId = projectId
            };
        }
    }

    public async Task<AnalysisSummary> AnalyzeAllRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var summary = new AnalysisSummary();

        try
        {
            _logger.LogInformation("Starting analysis of all repositories");

            // Get all projects from database
            var projects = (await _repositoryDataService.GetAllProjectsAsync(cancellationToken))
                .Where(p => p.AnalysisDate == null)
                .ToList();
            summary.TotalRepositories = projects.Count;

            foreach (var project in projects)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Analysis cancelled by user");
                    break;
                }

                try
                {
                    var result = await AnalyzeRepositoryAsync(project.Id, cancellationToken);
                    
                    if (result.IsSuccess)
                    {
                        summary.SuccessfulAnalyses++;
                        summary.TotalTokensUsed += result.TokensUsed;
                    }
                    else
                    {
                        summary.FailedAnalyses++;
                        summary.Errors.Add($"Project {project.Name}: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    summary.FailedAnalyses++;
                    summary.Errors.Add($"Project {project.Name}: {ex.Message}");
                    _logger.LogError(ex, "Error analyzing project {ProjectName}", project.Name);
                }

                // Small delay between analyses to avoid overwhelming APIs
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }

            stopwatch.Stop();
            summary.TotalDuration = stopwatch.Elapsed;

            _logger.LogInformation("Analysis of all repositories completed. Success: {Success}, Failed: {Failed}, Duration: {Duration}",
                summary.SuccessfulAnalyses, summary.FailedAnalyses, summary.TotalDuration);

            return summary;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during analysis of all repositories");
            
            summary.TotalDuration = stopwatch.Elapsed;
            summary.Errors.Add($"Global error: {ex.Message}");
            return summary;
        }
    }

    private async Task SaveDebugFilesAsync(string repositoryPath, RepositoryAnalysisResult result, CancellationToken cancellationToken)
    {
        try
        {
            var outputDir = _settings.DebugOutputDirectory;
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = repositoryPath; // Save in the repository directory by default
            }

            Directory.CreateDirectory(outputDir);

            // Save description markdown
            if (!string.IsNullOrEmpty(result.Description))
            {
                var descriptionPath = Path.Combine(outputDir, "README-GENERATED.md");
                await File.WriteAllTextAsync(descriptionPath, result.Description, cancellationToken);
                result.DescriptionFilePath = descriptionPath;
            }

            // Save one-liner
            if (!string.IsNullOrEmpty(result.Headline))
            {
                var headlinePath = Path.Combine(outputDir, "One-Liner.txt");
                await File.WriteAllTextAsync(headlinePath, result.Headline, cancellationToken);
                result.HeadlineFilePath = headlinePath;
            }

            // Save image prompt
            if (!string.IsNullOrEmpty(result.ImagePrompt))
            {
                var imagePromptPath = Path.Combine(outputDir, "Hero-Image-Prompt.txt");
                await File.WriteAllTextAsync(imagePromptPath, result.ImagePrompt, cancellationToken);
                result.ImagePromptFilePath = imagePromptPath;
            }

            // Save hero image
            if (result.HeroImage is { Length: > 0 })
            {
                var imagePath = Path.Combine(outputDir, "Hero-Image.png");
                await File.WriteAllBytesAsync(imagePath, result.HeroImage, cancellationToken);
                result.HeroImageFilePath = imagePath;
            }

            _logger.LogDebug("Debug files saved to: {OutputDir}", outputDir);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save debug files for repository: {RepositoryPath}", repositoryPath);
        }
    }
}