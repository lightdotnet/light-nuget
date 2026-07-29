using Light.Caching.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace UnitTests.CachingTests;

public class MemoryCacheServiceTests
{
    private static MemoryCacheService CreateService() =>
        new(new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryCacheService>.Instance);

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
    public void Get_TypeMismatch_ThrowsInvalidCastException()
    {
        var cache = CreateService();
        cache.Set("key", "a string value");

        Assert.Throws<InvalidCastException>(() => cache.Get<int>("key"));
    }

    [Test]
    public void TryGet_TypeMismatch_ReturnsDefault_DoesNotThrow()
    {
        var cache = CreateService();
        cache.Set("key", "a string value");

        Assert.DoesNotThrow(() => cache.TryGet<int>("key"));
        cache.TryGet<int>("key").ShouldBe(0);
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
