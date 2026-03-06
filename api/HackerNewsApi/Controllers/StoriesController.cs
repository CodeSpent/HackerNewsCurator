using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoriesController : ControllerBase
{
    private readonly IHackerNewsService _hackerNewsService;

    public StoriesController(IHackerNewsService hackerNewsService)
    {
        _hackerNewsService = hackerNewsService;
    }

    [HttpGet("newest")]
    public async Task<IActionResult> GetNewestStories([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool nocache = false)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var stories = await _hackerNewsService.GetNewestStoriesAsync(nocache);

        if (!string.IsNullOrWhiteSpace(search))
        {
            stories = stories.Where(s => s.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = stories.Count();
        var pagedStories = stories
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Ok(new
        {
            stories = pagedStories,
            totalCount,
            page,
            pageSize
        });
    }
}
