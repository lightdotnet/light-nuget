# Lightsoft.Caching

Cache abstraction and provider implementations for services that need a swappable in-process or distributed cache behind a single interface. Consumers depend on `ICacheService` (or the async-only `IAsyncCacheService`) and pick the backing provider — in-process `MemoryCache` or Redis via `Microsoft.Extensions.Caching.StackExchangeRedis` — purely through DI configuration, with no code change at the call site.

- **NuGet package id / assembly name:** `Lightsoft.Caching` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.Caching` — types live under `Light.Caching.Interfaces`, `Light.Caching.Infrastructure`, and `Light.Extensions.DependencyInjection`
- **Target framework:** net10.0
- **Dependencies:** `Microsoft.Extensions.Caching.Memory` and `Microsoft.Extensions.Caching.StackExchangeRedis` (both versioned via the shared `$(AspnetVersion)` MSBuild property). No `ProjectReference`s — this is a leaf project.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `IAsyncCacheService` | `Light.Caching.Interfaces` | Async cache contract: `GetAsync<T>`, `TryGetAsync<T>`, `SetAsync<T>` (with/without sliding expiration), `TrySetAsync<T>` (with/without sliding expiration), `RemoveAsync`. |
| `ICacheService` | `Light.Caching.Interfaces` | Sync cache contract extending `IAsyncCacheService`: `Get<T>`, `TryGet<T>`, `Set<T>` (with/without sliding expiration), `TrySet<T>` (with/without sliding expiration), `Remove`. |
| `IMemoryCache` | `Light.Caching.Interfaces` | Marker interface extending `ICacheService`, identifying an in-process cache implementation. Adds no members of its own. |
| `IDistributedCache` | `Light.Caching.Interfaces` | Marker interface extending `ICacheService`, identifying a distributed cache implementation. Adds no members of its own. |
| `MemoryCacheService` | `Light.Caching.Infrastructure` | `IMemoryCache` implementation wrapping `Microsoft.Extensions.Caching.Memory.IMemoryCache`. |
| `DistributedCacheService` | `Light.Caching.Infrastructure` | `IDistributedCache` implementation wrapping `Microsoft.Extensions.Caching.Distributed.IDistributedCache`; serializes/deserializes values to/from JSON via `System.Text.Json`. |
| `CacheOptions` | `Light.Caching.Infrastructure` | Configuration POCO: `Provider`, `RedisHost`, `RedisPassword` (all plain `string`). |
| `CacheDataExtensions` *(internal)* | `Light.Caching.Infrastructure` | `JsonSerialize<T>` / `ReadFromJson<T>` helpers used internally by `DistributedCacheService`, backed by a shared camelCase `JsonSerializerOptions`. |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddCache(Action<CacheOptions>)` and `AddCache(CacheOptions)` registration helpers. |

## How `AddCache` picks a provider

`AddCache(CacheOptions settings)` (the `Action<CacheOptions>` overload just builds a `CacheOptions` and delegates to this one) branches on `settings.Provider`:

- **`"redis"`** — throws a plain `Exception` if `settings.RedisHost` is null/empty. Otherwise builds a `StackExchange.Redis.ConfigurationOptions` (`AbortOnConnectFail = true`, single endpoint = `RedisHost`, `Password` set only if `RedisPassword` is non-empty), calls `services.AddStackExchangeRedisCache(...)` with it, and registers `ICacheService` → `DistributedCacheService` via `AddTransient`.
- **Anything else (including null/empty)** — treated as the default: calls `services.AddMemoryCache()` and registers `ICacheService` → `MemoryCacheService` via `AddTransient`.

Both branches register only the `ICacheService` interface — the `IMemoryCache`/`IDistributedCache` marker interfaces are not registered by `AddCache`, so consumers should inject `ICacheService` (or `IAsyncCacheService` if only the async members are needed), not the marker interfaces.

## Usage

### Register (configuration-driven)

```csharp
var settings = builder.Configuration.GetSection("Caching").Get<CacheOptions>();

builder.Services.AddCache(opt =>
{
    opt.Provider = settings!.Provider;       // "redis" or anything else -> memory
    opt.RedisHost = settings.RedisHost;
    opt.RedisPassword = settings.RedisPassword;
});
```

```json
// Memory (default) — Provider absent or anything other than "redis"
{ "Caching": { "Provider": "memory" } }

// Redis
{ "Caching": { "Provider": "redis", "RedisHost": "localhost:6379", "RedisPassword": "" } }
```

Or register directly without binding configuration:

```csharp
services.AddCache(new CacheOptions { Provider = "redis", RedisHost = "localhost:6379" });
```

### Consume

```csharp
public class CachingController(ICacheService cacheService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Load()
    {
        // Sync
        cacheService.Set("test_key", 42);
        var syncValue = cacheService.Get<int>("test_key");
        cacheService.Remove("test_key");

        // Async, with sliding expiration
        await cacheService.SetAsync("test_key", 42, TimeSpan.FromMinutes(5));
        var asyncValue = await cacheService.GetAsync<int>("test_key");
        await cacheService.RemoveAsync("test_key");

        return Ok(asyncValue);
    }
}
```

## Notes

- `MemoryCacheService`/`DistributedCacheService` implement `Interfaces.IMemoryCache`/`Interfaces.IDistributedCache` using the qualified name — both files also `using` the BCL's `Microsoft.Extensions.Caching.Memory`/`Distributed` namespaces (for the wrapped field type), which share the simple names `IMemoryCache`/`IDistributedCache` with this package's own interfaces. Don't be misled by an unqualified `IMemoryCache`/`IDistributedCache` in code that also references this package — check the `using`s to know which one is meant.
- `Try*` methods (`TryGet`, `TrySet`, `TryGetAsync`, `TrySetAsync`) catch all exceptions, log via the injected `ILogger<MemoryCacheService>`/`ILogger<DistributedCacheService>` (`LogError` with the key and exception message), and return `default`/complete silently. The plain `Get`/`Set`/`GetAsync`/`SetAsync` methods do **not** catch anything and propagate exceptions to the caller.
- A missing key generally does not throw on its own: `MemoryCacheService.Get<T>` delegates to `Microsoft.Extensions.Caching.Memory.IMemoryCache.Get<T>`, which returns `default(T)` for an absent key, and `DistributedCacheService.Get<T>` treats a null/empty `GetString` result as `default(T)` before attempting JSON deserialization (`CacheDataExtensions.ReadFromJson<T>`). The `Try*` variants mainly guard against other failures — e.g. a stored JSON payload that doesn't deserialize to the requested `T`, or the underlying cache provider (e.g. Redis connection) throwing.
- Only sliding expiration is exposed (`Set<T>(key, value, TimeSpan)` → `MemoryCacheEntryOptions.SlidingExpiration` or `DistributedCacheEntryOptions.SetSlidingExpiration`). There is no overload for absolute expiration.
- `AddCache` registers `ICacheService` with `AddTransient`, so a new `MemoryCacheService`/`DistributedCacheService` wrapper is created per resolution — cheap, since the wrapped `Microsoft.Extensions.Caching.*` cache itself is registered as a singleton by `AddMemoryCache`/`AddStackExchangeRedisCache`.
- `AddCache` throws a plain `System.Exception` (not a more specific exception type) when `Provider == "redis"` and `RedisHost` is missing — catch `Exception` broadly if you need to handle this at startup, or validate `CacheOptions` yourself beforehand.
- The project has `<Nullable>disable</Nullable>`, so `CacheOptions.Provider`/`RedisHost`/`RedisPassword` and the generic `T` return values (e.g. `TryGet<T>`'s `default`) carry no nullability annotations even though they're commonly absent/null at runtime.
