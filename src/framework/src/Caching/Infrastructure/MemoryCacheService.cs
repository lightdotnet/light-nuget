using Light.Exceptions;
using Light.Extensions.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Light.Infrastructure
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache,
            ILogger<MemoryCacheService> logger)
            => (_cache, _logger) = (cache, logger);

        public T? Get<T>(string key)
        {
            try
            {
                return _cache.Get<T>(key);
            }
            catch (InvalidCastException ex)
            {
                throw new CacheDeserializationException(key, typeof(T), ex);
            }
        }

        public T? TryGet<T>(string key)
        {
            try
            {
                return Get<T>(key);
            }
            catch (Exception ex)
            {
                _logger.LogError("Cache {key} GET error: {error}", key, ex.Message);
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? slidingExpiration = null)
        {
            if (!slidingExpiration.HasValue)
            {
                _cache.Set(key, value);
                return;
            }

            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration.Value
            };

            _cache.Set(key, value, options);
        }

        public void TrySet<T>(string key, T value, TimeSpan? slidingExpiration = null)
        {
            try
            {
                Set(key, value, slidingExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError("Cache {key} SET error: {error}", key, ex.Message);
            }
        }

        public void Remove(string key) => _cache.Remove(key);

        #region[Async]

        public Task<T?> GetAsync<T>(string key,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Get<T>(key));

        public async Task<T?> TryGetAsync<T>(string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetAsync<T>(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Cache {key} GET error: {error}", key, ex.Message);
                return default;
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default)
        {
            Set(key, value, slidingExpiration);
            return Task.CompletedTask;
        }

        public async Task TrySetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await SetAsync(key, value, slidingExpiration, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Cache {key} SET error: {error}", key, ex.Message);
            }
        }

        public Task RemoveAsync(string key,
            CancellationToken cancellationToken = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        #endregion
    }
}