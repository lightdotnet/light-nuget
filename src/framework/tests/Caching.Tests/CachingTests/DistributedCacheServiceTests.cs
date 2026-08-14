using Light.Exceptions;
using Light.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Text.Json;

namespace Caching.Tests.CachingTests;

public class DistributedCacheServiceTests
{
    private static DistributedCacheService CreateService() =>
        new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<DistributedCacheService>.Instance);

    [Test]
    public void Set_Get_RoundTrip_ReturnsStoredValue()
    {
        var cache = CreateService();

        cache.Set("key", 42);

        cache.Get<int>("key").ShouldBe(42);
    }

    [Test]
    public async Task SetAsync_GetAsync_RoundTrip_ReturnsStoredValue()
    {
        var cache = CreateService();

        await cache.SetAsync("key", "value");

        (await cache.GetAsync<string>("key")).ShouldBe("value");
    }

    [Test]
    public void Get_MissingKey_ReturnsDefault()
    {
        var cache = CreateService();

        cache.Get<int>("missing").ShouldBe(0);
        Assert.That(cache.Get<string?>("missing"), Is.Null);
    }

    [Test]
    public void Remove_ExistingKey_SubsequentGetReturnsDefault()
    {
        var cache = CreateService();
        cache.Set("key", 1);

        cache.Remove("key");

        cache.Get<int>("key").ShouldBe(0);
    }

    [Test]
    public async Task RemoveAsync_ExistingKey_SubsequentGetAsyncReturnsDefault()
    {
        // Regression test: RemoveAsync previously called _cache.RefreshAsync (a no-op reset of the
        // sliding-expiration timer) instead of _cache.RemoveAsync, so the entry was never removed.
        var cache = CreateService();
        await cache.SetAsync("key", 1);

        await cache.RemoveAsync("key");

        (await cache.GetAsync<int>("key")).ShouldBe(0);
    }

    [Test]
    public void Set_WithSlidingExpiration_ExpiresAfterDelay()
    {
        var cache = CreateService();

        cache.Set("key", "value", TimeSpan.FromMilliseconds(50));
        Thread.Sleep(200);

        Assert.That(cache.Get<string?>("key"), Is.Null);
    }

    [Test]
    public void Get_JsonTypeMismatch_ThrowsCacheDeserializationException()
    {
        var cache = CreateService();
        cache.Set("key", 42);

        var ex = Assert.Throws<CacheDeserializationException>(() => cache.Get<Guid>("key"));
        Assert.That(ex!.InnerException, Is.TypeOf<JsonException>());
    }

    [Test]
    public void TryGet_JsonTypeMismatch_ReturnsDefault_DoesNotThrow()
    {
        var cache = CreateService();
        cache.Set("key", 42);

        Assert.DoesNotThrow(() => cache.TryGet<Guid>("key"));
        cache.TryGet<Guid>("key").ShouldBe(Guid.Empty);
    }

    [Test]
    public void Set_NullKey_ThrowsArgumentNullException()
    {
        var cache = CreateService();

        Assert.Throws<ArgumentNullException>(() => cache.Set(null!, "value"));
    }

    [Test]
    public void TrySet_NullKey_DoesNotThrow()
    {
        var cache = CreateService();

        Assert.DoesNotThrow(() => cache.TrySet(null!, "value"));
    }
}
