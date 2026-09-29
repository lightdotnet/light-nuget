using Light.Extensions;
using Light.Extensions.Caching;
using Light.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

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
            cache.GetOrSetAsync<string>("key", (Func<Task<string?>>)null!));
    }

    [Test]
    public async Task GetOrSetAsync_ConcurrentMissesForSameKey_InvokesFactoryOnce()
    {
        var cache = CreateService();
        var key = "stampede-" + Guid.NewGuid();
        var factoryCalls = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => cache.GetOrSetAsync<string>(key, async () =>
            {
                Interlocked.Increment(ref factoryCalls);
                await release.Task;
                return "computed";
            })))
            .ToArray();

        // give every caller time to miss the cache and queue on the per-key lock
        await Task.Delay(100);
        release.SetResult();

        var results = await Task.WhenAll(tasks);

        factoryCalls.ShouldBe(1);
        Assert.That(results, Is.All.EqualTo("computed"));
    }

    [Test]
    public async Task GetOrSetAsync_LockPerKeyDisabled_EachConcurrentMissInvokesFactory()
    {
        var cache = CreateService();
        var key = "nolock-" + Guid.NewGuid();
        var factoryCalls = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => cache.GetOrSetAsync<string>(key, async _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                await release.Task;
                return "computed";
            }, slidingExpiration: null, lockPerKey: false))
            .ToArray();

        release.SetResult();
        await Task.WhenAll(tasks);

        factoryCalls.ShouldBe(5);
    }

    [Test]
    public async Task GetOrSetAsync_CancellationTokenOverload_PassesTokenToFactory()
    {
        var cache = CreateService();
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;

        await cache.GetOrSetAsync<string>("key", ct =>
        {
            received = ct;
            return Task.FromResult<string?>("computed");
        }, cancellationToken: cts.Token);

        Assert.That(received, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetOrSetAsync_CanceledWhileWaitingForKeyLock_ThrowsOperationCanceledException()
    {
        var cache = CreateService();
        var key = "cancel-" + Guid.NewGuid();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = cache.GetOrSetAsync<string>(key, async () =>
        {
            await release.Task;
            return "computed";
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        Assert.CatchAsync<OperationCanceledException>(() =>
            cache.GetOrSetAsync<string>(key, () => Task.FromResult<string?>("other"), cancellationToken: cts.Token));

        release.SetResult();
        (await first).ShouldBe("computed");
    }
}
