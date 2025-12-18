namespace Light.Extensions.Caching
{
    public interface IAsyncCacheService
    {
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        Task<T> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);

        Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

        Task SetAsync<T>(string key, T value, TimeSpan slidingExpiration, CancellationToken cancellationToken = default);

        Task TrySetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

        Task TrySetAsync<T>(string key, T value, TimeSpan slidingExpiration, CancellationToken cancellationToken = default);

        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}


