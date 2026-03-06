using HackerNewsApi.Models;

namespace HackerNewsApi.Services;

public interface IHackerNewsService
{
    Task<IEnumerable<Story>> GetNewestStoriesAsync(bool skipCache = false);
}
