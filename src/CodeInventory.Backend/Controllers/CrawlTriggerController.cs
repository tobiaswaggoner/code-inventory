using CodeInventory.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeInventory.Backend.Controllers;

public class CrawlTriggerController : ControllerBase
{
    private readonly ICrawlTriggerService _crawlTriggerService;

    public CrawlTriggerController(ICrawlTriggerService crawlTriggerService)
    {
        _crawlTriggerService = crawlTriggerService;
    }

    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerCrawlAsync()
    {
        await _crawlTriggerService.TriggerCrawlAsync();
        return Ok();
    }

    [HttpGet("status")]
    public IActionResult GetCrawlStatus()
    {
        var isCrawling = _crawlTriggerService.IsCrawling;
        return Ok(new { IsCrawling = isCrawling });
    }
    
}