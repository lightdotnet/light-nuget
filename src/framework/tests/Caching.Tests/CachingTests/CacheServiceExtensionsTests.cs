using Light.Extensions;
using Light.Extensions.Caching;
using Light.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Caching.Tests.CachingTests;

public class CacheServiceExtensionsTests
{
    private static MemoryCacheService CreateService() =>
        new(new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryCacheService>.Instance);

    [Test]
    public async Task GetOrSetAsync_KeyMissing_InvokesFactoryAndCachesResult()
    {
        var cache = CreateService();
        var factoryCalls = 0;

        var result = await cache.GetOrSetAsync("key", () =>
        {
            factoryCalls++;
            return Task.FromResult<string?>("computed");
        });

        result.ShouldBe("computed");
        factoryCalls.ShouldBe(1);
        cache.Get<string?>("key").ShouldBe("computed");
    }

    [Test]
    public async Task GetOrSetAsync_KeyPresent_ReturnsCachedValue_DoesNotInvokeFactory()
    {
        var cache = CreateService();
        cache.Set("key", "cached");
        var factoryCalls = 0;

        var result = await cache.GetOrSetAsync("key", () =>
        {
            factoryCalls++;
            return Task.FromResult<string?>("computed");
        });

        result.ShouldBe("cached");
        factoryCalls.ShouldBe(0);
    }

    [Test]
    public async Task GetOrSetAsync_WithSlidingExpiration_SetsWithExpiration()
    {
        var cache = CreateService();

        var result = await cache.GetOrSetAsync("key", () => Task.FromResult<string?>("computed"),
            TimeSpan.FromMilliseconds(50));

        result.ShouldBe("computed");
        Thread.Sleep(200);
        Assert.That(cache.Get<string?>("key"), Is.Null);
    }

    [Test]
    public async Task GetOrSetAsync_FactoryReturnsNull_ReturnsDefault_DoesNotCache()
    {
        var cache = CreateService();
        var factoryCalls = 0;

        var result = await cache.GetOrSetAsync<string>("key", () =>
        {
            factoryCalls++;
            return Task.FromResult<string?>(null);
        });

        Assert.That(result, Is.Null);
        factoryCalls.ShouldBe(1);

        // key was never cached, so a second call invokes the factory again
        await cache.GetOrSetAsync<string>("key", () =>
        {
            factoryCalls++;
            return Task.FromResult<string?>(null);
        });
        factoryCalls.ShouldBe(2);
    }

    [Test]
    public void GetOrSetAsync_NullCacheService_ThrowsArgumentNullException()
    {
        ICacheService? cache = null;

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            cache!.GetOrSetAsync("key", () => Task.FromResult<string?>("computed")));
    }

    [Test]
    public void GetOrSetAsync_NullFactory_ThrowsArgumentNullException()
    {
        var cache = CreateService();

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            cache.GetOrSetAsync<string>("key", null!));
    }
}
