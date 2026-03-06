using System.Net;
using System.Text.Json;
using HackerNewsApi.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsApi.Tests;

public class HackerNewsServiceTests
{
    private static HttpClient CreateMockHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler);
    }

    [Fact]
    public async Task GetNewestStoriesAsync_ReturnsCachedStories_WhenCacheIsPopulated()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var expectedStories = new List<Models.Story>
        {
            new() { Id = 1, Title = "Cached Story", Url = "https://example.com" }
        };
        cache.Set("newest_stories", (IEnumerable<Models.Story>)expectedStories, TimeSpan.FromMinutes(5));

        var handler = new FakeHttpMessageHandler(new Dictionary<string, string>());
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        var result = await service.GetNewestStoriesAsync();

        Assert.Single(result);
        Assert.Equal("Cached Story", result.First().Title);
    }

    [Fact]
    public async Task GetNewestStoriesAsync_FetchesFromApi_WhenCacheIsEmpty()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storyIds = JsonSerializer.Serialize(new[] { 1, 2 });
        var story1 = JsonSerializer.Serialize(new { id = 1, title = "Story One", url = "https://one.com", time = 1709600000, by = "user1" });
        var story2 = JsonSerializer.Serialize(new { id = 2, title = "Story Two", url = (string?)null, time = 1709600100, by = "user2" });

        var responses = new Dictionary<string, string>
        {
            ["https://hacker-news.firebaseio.com/v0/newstories.json"] = storyIds,
            ["https://hacker-news.firebaseio.com/v0/item/1.json"] = story1,
            ["https://hacker-news.firebaseio.com/v0/item/2.json"] = story2
        };

        var handler = new FakeHttpMessageHandler(responses);
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        var result = (await service.GetNewestStoriesAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Story One", result[0].Title);
        Assert.Equal("https://one.com", result[0].Url);
        Assert.Equal("Story Two", result[1].Title);
        Assert.Null(result[1].Url);
    }

    [Fact]
    public async Task GetNewestStoriesAsync_ReturnsEmpty_WhenApiReturnsNoIds()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var responses = new Dictionary<string, string>
        {
            ["https://hacker-news.firebaseio.com/v0/newstories.json"] = "[]"
        };

        var handler = new FakeHttpMessageHandler(responses);
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        var result = await service.GetNewestStoriesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNewestStoriesAsync_PopulatesCache_AfterFetch()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storyIds = JsonSerializer.Serialize(new[] { 1 });
        var story1 = JsonSerializer.Serialize(new { id = 1, title = "Test", url = "https://test.com", time = 1709600000, by = "tester" });

        var responses = new Dictionary<string, string>
        {
            ["https://hacker-news.firebaseio.com/v0/newstories.json"] = storyIds,
            ["https://hacker-news.firebaseio.com/v0/item/1.json"] = story1
        };

        var handler = new FakeHttpMessageHandler(responses);
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        await service.GetNewestStoriesAsync();

        Assert.True(cache.TryGetValue("newest_stories", out _));
    }

    [Fact]
    public async Task GetNewestStoriesAsync_SkipsFailedStoryFetches()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storyIds = JsonSerializer.Serialize(new[] { 1, 2, 3 });
        var story1 = JsonSerializer.Serialize(new { id = 1, title = "Good Story", url = "https://good.com", time = 1709600000, by = "user1" });
        var story3 = JsonSerializer.Serialize(new { id = 3, title = "Another Good", url = "https://good2.com", time = 1709600200, by = "user3" });

        // Story 2 is missing from responses, so FakeHttpMessageHandler returns 404
        var responses = new Dictionary<string, string>
        {
            ["https://hacker-news.firebaseio.com/v0/newstories.json"] = storyIds,
            ["https://hacker-news.firebaseio.com/v0/item/1.json"] = story1,
            ["https://hacker-news.firebaseio.com/v0/item/3.json"] = story3
        };

        var handler = new FakeHttpMessageHandler(responses);
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        var result = (await service.GetNewestStoriesAsync()).ToList();

        // Story 2 should be skipped, only 1 and 3 returned
        Assert.Equal(2, result.Count);
        Assert.Equal("Good Story", result[0].Title);
        Assert.Equal("Another Good", result[1].Title);
    }

    [Fact]
    public async Task GetNewestStoriesAsync_ReturnsEmpty_WhenApiReturnsNull()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var responses = new Dictionary<string, string>
        {
            ["https://hacker-news.firebaseio.com/v0/newstories.json"] = "null"
        };

        var handler = new FakeHttpMessageHandler(responses);
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        var result = await service.GetNewestStoriesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNewestStoriesAsync_SecondCallUsesCacheNotApi()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storyIds = JsonSerializer.Serialize(new[] { 1 });
        var story1 = JsonSerializer.Serialize(new { id = 1, title = "Story", url = "https://test.com", time = 1709600000, by = "user" });

        var handler = new TrackingHttpMessageHandler(new Dictionary<string, string>
        {
            ["https://hacker-news.firebaseio.com/v0/newstories.json"] = storyIds,
            ["https://hacker-news.firebaseio.com/v0/item/1.json"] = story1
        });
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        await service.GetNewestStoriesAsync();
        var callsAfterFirst = handler.RequestCount;

        await service.GetNewestStoriesAsync();
        var callsAfterSecond = handler.RequestCount;

        // Second call should not make any additional HTTP requests
        Assert.Equal(callsAfterFirst, callsAfterSecond);
    }

    [Fact]
    public async Task GetNewestStoriesAsync_LimitsTo200Stories()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var ids = Enumerable.Range(1, 250).ToArray();
        var storyIds = JsonSerializer.Serialize(ids);

        var responses = new Dictionary<string, string>
        {
            ["https://hacker-news.firebaseio.com/v0/newstories.json"] = storyIds
        };

        // Add responses for all 250 story IDs
        foreach (var id in ids)
        {
            responses[$"https://hacker-news.firebaseio.com/v0/item/{id}.json"] =
                JsonSerializer.Serialize(new { id, title = $"Story {id}", url = $"https://example.com/{id}", time = 1709600000, by = "user" });
        }

        var handler = new TrackingHttpMessageHandler(responses);
        var httpClient = CreateMockHttpClient(handler);
        var service = new HackerNewsService(httpClient, cache);

        var result = (await service.GetNewestStoriesAsync()).ToList();

        Assert.Equal(200, result.Count);
        // Should only fetch newstories.json + 200 individual stories = 201 requests
        Assert.Equal(201, handler.RequestCount);
    }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses;

    public FakeHttpMessageHandler(Dictionary<string, string> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        if (_responses.TryGetValue(url, out var content))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public class TrackingHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses;
    private int _requestCount;

    public int RequestCount => _requestCount;

    public TrackingHttpMessageHandler(Dictionary<string, string> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);
        var url = request.RequestUri!.ToString();
        if (_responses.TryGetValue(url, out var content))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
