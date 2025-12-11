using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Light.Caching.Infrastructure
{
    public class DistributedCacheService : Extensions.Caching.IDistributedCache
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheService> _logger;

        public DistributedCacheService(IDistributedCache cache,
            ILogger<DistributedCacheService> logger)
            => (_cache, _logger) = (cache, logger);

        public T Get<T>(string key) => _cache.GetString(key).ReadFromJson<T>();

        public T TryGet<T>(string key)
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

        public void Set<T>(string key, T value) => _cache.SetString(key, value.JsonSerialize());

        public void TrySet<T>(string key, T value)
        {
            try
            {
                Set(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError("Cache {key} SET error: {error}", key, ex.Message);
            }
        }

        public void Set<T>(string key, T value, TimeSpan slidingExpiration)
        {
            var options = new DistributedCacheEntryOptions();

            options.SetSlidingExpiration(slidingExpiration);

            _cache.SetString(key, value.JsonSerialize(), options);
        }

        public void TrySet<T>(string key, T value, TimeSpan slidingExpiration)
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

        public async Task<T> GetAsync<T>(string key,
            CancellationToken cancellationToken = default)
        {
            var cacheValue = await _cache.GetStringAsync(key, cancellationToken);
            return cacheValue.ReadFromJson<T>();
        }

        public async Task<T> TryGetAsync<T>(string key,
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

        public Task SetAsync<T>(string key, T value,
            CancellationToken cancellationToken = default)
            => _cache.SetStringAsync(key, value.JsonSerialize(), cancellationToken);

        public async Task TrySetAsync<T>(string key, T value,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await GetAsync<T>(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Cache {key} SET error: {error}", key, ex.Message);
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan slidingExpiration,
            CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions();

            options.SetSlidingExpiration(slidingExpiration);

            return _cache.SetStringAsync(key, value.JsonSerialize(), options, cancellationToken);
        }

        public async Task TrySetAsync<T>(string key, T value, TimeSpan slidingExpiration,
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
            => _cache.RefreshAsync(key, cancellationToken);

        #endregion
    }
}