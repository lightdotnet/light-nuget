using Light.Exceptions;
using Light.Extensions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Light.Infrastructure
{
    public class DistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheService> _logger;

        public DistributedCacheService(IDistributedCache cache,
            ILogger<DistributedCacheService> logger)
            => (_cache, _logger) = (cache, logger);

        public T? Get<T>(string key)
        {
            try
            {
                return _cache.GetString(key).ReadFromJson<T>();
            }
            catch (JsonException ex)
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
                _cache.SetString(key, value.JsonSerialize());
                return;
            }

            var options = new DistributedCacheEntryOptions();
            options.SetSlidingExpiration(slidingExpiration.Value);

            _cache.SetString(key, value.JsonSerialize(), options);
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

        public async Task<T?> GetAsync<T>(string key,
            CancellationToken cancellationToken = default)
        {
            var cacheValue = await _cache.GetStringAsync(key, cancellationToken);

            try
            {
                return cacheValue.ReadFromJson<T>();
            }
            catch (JsonException ex)
            {
                throw new CacheDeserializationException(key, typeof(T), ex);
            }
        }

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
            if (!slidingExpiration.HasValue)
                return _cache.SetStringAsync(key, value.JsonSerialize(), cancellationToken);

            var options = new DistributedCacheEntryOptions();
            options.SetSlidingExpiration(slidingExpiration.Value);

            return _cache.SetStringAsync(key, value.JsonSerialize(), options, cancellationToken);
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
            => _cache.RemoveAsync(key, cancellationToken);

        #endregion
    }
}