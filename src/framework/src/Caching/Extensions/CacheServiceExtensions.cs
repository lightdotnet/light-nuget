using Light.Extensions.Caching;

namespace Light.Extensions
{
    public static class CacheServiceExtensions
    {
        // in-process, per-key locks used by GetOrSetAsync to prevent a cache stampede;
        // entries are reference counted and removed as soon as no caller holds/awaits them
        private static readonly Dictionary<string, KeyedLock> KeyedLocks = new(StringComparer.Ordinal);

        /// <summary>
        /// Returns the cached value for <paramref name="key"/>, or invokes <paramref name="factory"/>
        /// and returns it when the key is missing. A non-null factory result is cached (with
        /// <paramref name="slidingExpiration"/> when provided); a null result is returned as-is
        /// without being cached.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Concurrent misses for the same <paramref name="key"/> within the current process are
        /// serialized with a per-key lock, so <paramref name="factory"/> runs once and the other
        /// callers read the freshly cached value. This does not coordinate across processes/servers.
        /// </para>
        /// <para>
        /// For a value type <typeparamref name="T"/>, a cached value equal to <c>default(T)</c> is
        /// indistinguishable from a cache miss (see <see cref="ICacheService.Get{T}"/>), so
        /// <paramref name="factory"/> is invoked again in that case.
        /// </para>
        /// </remarks>
        public static async Task<T?> GetOrSetAsync<T>(
            this ICacheService cacheService,
            string key,
            Func<Task<T?>> factory,
            TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(factory);

            return await cacheService.GetOrSetAsync(key, _ => factory(), slidingExpiration, lockPerKey: true, cancellationToken);
        }

        /// <summary>
        /// Returns the cached value for <paramref name="key"/>, or invokes <paramref name="factory"/>
        /// (passing <paramref name="cancellationToken"/>) and returns it when the key is missing.
        /// Concurrent misses for the same key within the current process run the factory once.
        /// </summary>
        /// <remarks>See <see cref="GetOrSetAsync{T}(ICacheService, string, Func{Task{T}}, TimeSpan?, CancellationToken)"/>.</remarks>
        public static async Task<T?> GetOrSetAsync<T>(
            this ICacheService cacheService,
            string key,
            Func<CancellationToken, Task<T?>> factory,
            TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default)
            => await cacheService.GetOrSetAsync(key, factory, slidingExpiration, lockPerKey: true, cancellationToken);

        /// <summary>
        /// Returns the cached value for <paramref name="key"/>, or invokes <paramref name="factory"/>
        /// (passing <paramref name="cancellationToken"/>) and returns it when the key is missing.
        /// </summary>
        /// <param name="lockPerKey">
        /// <c>true</c> to serialize concurrent misses for the same key within the current process so
        /// the factory runs once (stampede protection); <c>false</c> to let every caller run the factory.
        /// The lock is not re-entrant: a factory must not call <c>GetOrSetAsync</c> for the same key
        /// with locking enabled.
        /// </param>
        public static async Task<T?> GetOrSetAsync<T>(
            this ICacheService cacheService,
            string key,
            Func<CancellationToken, Task<T?>> factory,
            TimeSpan? slidingExpiration,
            bool lockPerKey,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(cacheService);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(factory);

            var cached = await cacheService.GetAsync<T>(key, cancellationToken);
            if (cached is not null)
                return cached;

            if (!lockPerKey)
                return await InvokeFactoryAndSetAsync(cacheService, key, factory, slidingExpiration, cancellationToken);

            var keyedLock = AcquireKeyedLock(key);
            try
            {
                await keyedLock.Semaphore.WaitAsync(cancellationToken);
                try
                {
                    // another caller may have populated the key while we were waiting
                    cached = await cacheService.GetAsync<T>(key, cancellationToken);
                    if (cached is not null)
                        return cached;

                    return await InvokeFactoryAndSetAsync(cacheService, key, factory, slidingExpiration, cancellationToken);
                }
                finally
                {
                    keyedLock.Semaphore.Release();
                }
            }
            finally
            {
                ReleaseKeyedLock(key, keyedLock);
            }
        }

        private static async Task<T?> InvokeFactoryAndSetAsync<T>(
            ICacheService cacheService,
            string key,
            Func<CancellationToken, Task<T?>> factory,
            TimeSpan? slidingExpiration,
            CancellationToken cancellationToken)
        {
            var value = await factory(cancellationToken);
            if (value is null)
                return default;

            await cacheService.SetAsync(key, value, slidingExpiration, cancellationToken);

            return value;
        }

        private static KeyedLock AcquireKeyedLock(string key)
        {
            lock (KeyedLocks)
            {
                if (!KeyedLocks.TryGetValue(key, out var keyedLock))
                {
                    keyedLock = new KeyedLock();
                    KeyedLocks[key] = keyedLock;
                }

                keyedLock.RefCount++;
                return keyedLock;
            }
        }

        private static void ReleaseKeyedLock(string key, KeyedLock keyedLock)
        {
            lock (KeyedLocks)
            {
                if (--keyedLock.RefCount == 0)
                {
                    KeyedLocks.Remove(key);
                    keyedLock.Semaphore.Dispose();
                }
            }
        }

        private sealed class KeyedLock
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);

            public int RefCount { get; set; }
        }
    }
}
