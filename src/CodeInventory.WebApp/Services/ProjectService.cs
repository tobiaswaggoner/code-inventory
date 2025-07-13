using CodeInventory.WebApp.Models;
using System.Text.Json;

namespace CodeInventory.WebApp.Services;

public class ProjectService : IProjectService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProjectService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProjectService(HttpClient httpClient, ILogger<ProjectService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/projects", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var projects = JsonSerializer.Deserialize<List<ProjectSummaryDto>>(json, _jsonOptions);
            
            return projects ?? new List<ProjectSummaryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects from API");
            return new List<ProjectSummaryDto>();
        }
    }

    public async Task<byte[]?> GetProjectImageAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/projects/{projectId}/image", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving project image for ID {ProjectId}", projectId);
            return null;
        }
    }
}