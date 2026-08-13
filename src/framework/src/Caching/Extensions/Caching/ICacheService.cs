namespace Light.Extensions.Caching
{
    public interface ICacheService : IAsyncCacheService
    {
        T? Get<T>(string key);

        T? TryGet<T>(string key);

        void Set<T>(string key, T value, TimeSpan? slidingExpiration = null);

        void TrySet<T>(string key, T value, TimeSpan? slidingExpiration = null);

        void Remove(string key);
    }
}


