namespace Light.Extensions.Caching
{
    public interface ICacheService : IAsyncCacheService
    {
        //[return: MaybeNull]
        T Get<T>(string key);

        T TryGet<T>(string key);

        void Set<T>(string key, T value);

        void Set<T>(string key, T value, TimeSpan slidingExpiration);

        void TrySet<T>(string key, T value);

        void TrySet<T>(string key, T value, TimeSpan slidingExpiration);

        void Remove(string key);
    }
}


