using HackerNewsApi.Models;
using HackerNewsApi.Services;
using HackerNewsApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HackerNewsApi.Tests;

public class StoriesControllerTests
{
    private readonly Mock<IHackerNewsService> _mockService;
    private readonly StoriesController _controller;

    public StoriesControllerTests()
    {
        _mockService = new Mock<IHackerNewsService>();
        _controller = new StoriesController(_mockService.Object);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsOkWithStories()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "Story 1", Url = "https://example.com/1" },
            new() { Id = 2, Title = "Story 2", Url = "https://example.com/2" },
            new() { Id = 3, Title = "Story 3", Url = null }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories(null, 1, 20) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetNewestStories_FiltersStoriesBySearch()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "Angular Tutorial", Url = "https://example.com/1" },
            new() { Id = 2, Title = "React Guide", Url = "https://example.com/2" },
            new() { Id = 3, Title = "Angular Best Practices", Url = "https://example.com/3" }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories("Angular", 1, 20) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":2", json);
    }

    [Fact]
    public async Task GetNewestStories_PagesResultsCorrectly()
    {
        var stories = Enumerable.Range(1, 50)
            .Select(i => new Story { Id = i, Title = $"Story {i}" })
            .ToList();
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories(null, 2, 10) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":50", json);
        Assert.Contains("\"page\":2", json);
    }

    [Fact]
    public async Task GetNewestStories_ClampsInvalidPageSize()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "Story 1" }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories(null, -1, 0) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"page\":1", json);
        Assert.Contains("\"pageSize\":20", json);
    }

    [Fact]
    public async Task GetNewestStories_SearchIsCaseInsensitive()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "ANGULAR Tutorial" },
            new() { Id = 2, Title = "React Guide" }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories("angular", 1, 20) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":1", json);
    }

    [Fact]
    public async Task GetNewestStories_ClampsPageSizeAbove100()
    {
        var stories = Enumerable.Range(1, 5)
            .Select(i => new Story { Id = i, Title = $"Story {i}" })
            .ToList();
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories(null, 1, 200) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"pageSize\":100", json);
    }

    [Fact]
    public async Task GetNewestStories_EmptySearchReturnsAll()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "Story 1" },
            new() { Id = 2, Title = "Story 2" }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories("", 1, 20) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":2", json);
    }

    [Fact]
    public async Task GetNewestStories_WhitespaceSearchReturnsAll()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "Story 1" },
            new() { Id = 2, Title = "Story 2" }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories("   ", 1, 20) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":2", json);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsEmptyWhenNoStories()
    {
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(new List<Story>());

        var result = await _controller.GetNewestStories(null, 1, 20) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":0", json);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsCorrectStoriesForPage()
    {
        var stories = Enumerable.Range(1, 30)
            .Select(i => new Story { Id = i, Title = $"Story {i}" })
            .ToList();
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories(null, 2, 10) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        // Page 2 with pageSize 10 should contain stories 11-20
        Assert.Contains("\"Title\":\"Story 11\"", json);
        Assert.Contains("\"Title\":\"Story 20\"", json);
        Assert.DoesNotContain("\"Title\":\"Story 10\"", json);
        Assert.DoesNotContain("\"Title\":\"Story 21\"", json);
    }

    [Fact]
    public async Task GetNewestStories_SearchAndPaginationWorkTogether()
    {
        var stories = Enumerable.Range(1, 25)
            .Select(i => new Story { Id = i, Title = i % 2 == 0 ? $"Angular {i}" : $"React {i}" })
            .ToList();
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        // 12 Angular stories (2,4,6,...,24), page 2 with pageSize 5 should give stories at index 5-9
        var result = await _controller.GetNewestStories("Angular", 2, 5) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":12", json);
        Assert.Contains("\"page\":2", json);
        Assert.Contains("\"pageSize\":5", json);
    }

    [Fact]
    public async Task GetNewestStories_PassesNocacheToService()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "Story 1" }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(true)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories(null, 1, 20, nocache: true) as OkObjectResult;

        Assert.NotNull(result);
        _mockService.Verify(s => s.GetNewestStoriesAsync(true), Times.Once);
    }

    [Fact]
    public async Task GetNewestStories_PageBeyondResultsReturnsEmpty()
    {
        var stories = new List<Story>
        {
            new() { Id = 1, Title = "Only Story" }
        };
        _mockService.Setup(s => s.GetNewestStoriesAsync(false)).ReturnsAsync(stories);

        var result = await _controller.GetNewestStories(null, 5, 20) as OkObjectResult;

        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"totalCount\":1", json);
        // Stories array should be empty since page 5 is beyond results
        Assert.Contains("\"stories\":[]", json);
    }
}
