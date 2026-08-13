using Light.Extensions.Caching;

namespace Light.Extensions
{
    public static class CacheServiceExtensions
    {
        /// <summary>
        /// Returns the cached value for <paramref name="key"/>, or invokes <paramref name="factory"/>
        /// and returns it when the key is missing. A non-null factory result is cached (with
        /// <paramref name="slidingExpiration"/> when provided); a null result is returned as-is
        /// without being cached.
        /// </summary>
        /// <remarks>
        /// For a value type <typeparamref name="T"/>, a cached value equal to <c>default(T)</c> is
        /// indistinguishable from a cache miss (see <see cref="ICacheService.Get{T}"/>), so
        /// <paramref name="factory"/> is invoked again in that case.
        /// </remarks>
        public static async Task<T?> GetOrSetAsync<T>(
            this ICacheService cacheService,
            string key,
            Func<Task<T?>> factory,
            TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(cacheService);
            ArgumentNullException.ThrowIfNull(factory);

            var cached = await cacheService.GetAsync<T>(key, cancellationToken);
            if (cached is not null)
                return cached;

            var value = await factory();
            if (value is null)
                return default;

            await cacheService.SetAsync(key, value, slidingExpiration, cancellationToken);

            return value;
        }
    }
}
