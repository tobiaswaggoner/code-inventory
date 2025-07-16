using CodeInventory.Backend.DTOs;
using CodeInventory.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeInventory.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger;
    private readonly IRepositoryDataService _repositoryDataService;

    public ProjectsController(ILogger<ProjectsController> logger, IRepositoryDataService repositoryDataService)
    {
        _logger = logger;
        _repositoryDataService = repositoryDataService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectSummaryDto>>> GetProjects(CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _repositoryDataService.GetAllProjectsAsync(cancellationToken);
            
            var projectSummaries = projects
                .OrderBy(p => p.Name)
                .Select(p => new ProjectSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Headline = p.Headline,
                    Description = p.Description,
                    HasHeroImage = p.HeroImage != null && p.HeroImage.Length > 0,
                    AnalysisDate = p.AnalysisDate,
                    CommitCount = p.Commits?.Count ?? 0,
                    LocationCount = p.Locations?.Count ?? 0
                })
                .ToList();

            return Ok(projectSummaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id:guid}/image")]
    public async Task<ActionResult> GetProjectImage(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var project = await _repositoryDataService.GetProjectByIdAsync(id, cancellationToken);
            
            if (project == null)
            {
                return NotFound();
            }

            if (project.HeroImage == null || project.HeroImage.Length == 0)
            {
                return NotFound();
            }

            return File(project.HeroImage, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving project image for ID {ProjectId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}