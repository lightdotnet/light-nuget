using Light.Extensions.Caching;
using Light.Extensions.DependencyInjection;
using Light.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Tests.CachingTests;

public class ServiceCollectionExtensionsTests
{
    private static Type? RegisteredCacheImplementation(IServiceCollection services) =>
        services.Single(d => d.ServiceType == typeof(ICacheService)).ImplementationType;

    [TestCase("redis")]
    [TestCase("Redis")]
    [TestCase("REDIS")]
    public void AddCache_RedisProvider_IsCaseInsensitive(string provider)
    {
        var services = new ServiceCollection();

        services.AddCache(new CacheOptions { Provider = provider, RedisHost = "localhost:6379" });

        RegisteredCacheImplementation(services).ShouldBe(typeof(DistributedCacheService));
    }

    [Test]
    public void AddCache_DefaultProvider_RegistersMemoryCacheService()
    {
        var services = new ServiceCollection();

        services.AddCache(new CacheOptions());

        RegisteredCacheImplementation(services).ShouldBe(typeof(MemoryCacheService));
    }

    [Test]
    public void AddCache_RedisProviderWithoutHost_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddCache(new CacheOptions { Provider = "Redis" }));

        Assert.That(ex!.Message, Does.Contain(nameof(CacheOptions.RedisHost)));
    }
}
