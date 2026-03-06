using System.Net.Http.Json;
using HackerNewsApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsApi.Services;

public class HackerNewsService : IHackerNewsService
{
    private const string BaseUrl = "https://hacker-news.firebaseio.com/v0";
    private const string CacheKey = "newest_stories";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<IEnumerable<Story>> GetNewestStoriesAsync(bool skipCache = false)
    {
        if (!skipCache && _cache.TryGetValue(CacheKey, out IEnumerable<Story>? cachedStories) && cachedStories is not null)
        {
            return cachedStories;
        }

        await CacheLock.WaitAsync();
        try
        {
            if (!skipCache && _cache.TryGetValue(CacheKey, out cachedStories) && cachedStories is not null)
            {
                return cachedStories;
            }

            var storyIds = await _httpClient.GetFromJsonAsync<int[]>($"{BaseUrl}/newstories.json");

            if (storyIds is null || storyIds.Length == 0)
            {
                return [];
            }

            var tasks = storyIds.Take(200).Select(id => FetchStoryAsync(id));
            var stories = await Task.WhenAll(tasks);

            var result = stories
                .Where(s => s is not null)
                .Cast<Story>()
                .ToList();

            _cache.Set(CacheKey, result, CacheDuration);

            return result;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private async Task<Story?> FetchStoryAsync(int id)
    {
        try
        {
            var item = await _httpClient.GetFromJsonAsync<HackerNewsItem>($"{BaseUrl}/item/{id}.json");

            if (item is null)
                return null;

            return new Story
            {
                Id = item.Id,
                Title = item.Title ?? string.Empty,
                Url = item.Url,
                Time = item.Time,
                By = item.By ?? string.Empty
            };
        }
        catch
        {
            return null;
        }
    }

    private class HackerNewsItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public long Time { get; set; }
        public string? By { get; set; }
    }
}
