using CodeInventory.WebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeInventory.WebApp.Controllers;

[ApiController]
[Route("image")]
public class ImageController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IProjectService projectService, ILogger<ImageController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }
    [HttpGet("{projectId}")]
    public async Task<ActionResult> GetImage(Guid projectId)
    {
        var response = await _projectService.GetProjectImageAsync(projectId);
            
        if (response == null)
        {
            return NotFound();
        }
        
        return File(response, "image/png");
    }
}