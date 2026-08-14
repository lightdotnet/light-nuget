[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.Caching

Cache abstraction and provider implementations for services that need a swappable in-process or distributed cache behind a single interface. Consumers depend on `ICacheService` (or the async-only `IAsyncCacheService`) and pick the backing provider — in-process `MemoryCache` or Redis via `Microsoft.Extensions.Caching.StackExchangeRedis` — purely through DI configuration, with no code change at the call site.

- **NuGet package id / assembly name:** `Lightsoft.Caching` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light` (bare — `RootNamespace` is `$(BaseNamespace)`, not `$(BaseNamespace).Caching`). Types live under `Light.Extensions.Caching`, `Light.Infrastructure`, `Light.Exceptions`, `Light.Extensions`, and `Light.Extensions.DependencyInjection` — **these are shared with other framework packages** (see the "Namespace layout" note below).
- **Target framework:** net10.0
- **Dependencies:** `Microsoft.Extensions.Caching.Memory` and `Microsoft.Extensions.Caching.StackExchangeRedis` (both versioned via the shared `$(AspnetVersion)` MSBuild property). No `ProjectReference`s — this is a leaf project.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `IAsyncCacheService` | `Light.Extensions.Caching` | Async cache contract: `GetAsync<T>`, `TryGetAsync<T>`, `SetAsync<T>(key, value, TimeSpan? slidingExpiration = null, ...)`, `TrySetAsync<T>(key, value, TimeSpan? slidingExpiration = null, ...)`, `RemoveAsync`. |
| `ICacheService` | `Light.Extensions.Caching` | Sync cache contract extending `IAsyncCacheService`: `Get<T>`, `TryGet<T>`, `Set<T>(key, value, TimeSpan? slidingExpiration = null)`, `TrySet<T>(key, value, TimeSpan? slidingExpiration = null)`, `Remove`. |
| `MemoryCacheService` | `Light.Infrastructure` | `ICacheService` implementation wrapping `Microsoft.Extensions.Caching.Memory.IMemoryCache`. |
| `DistributedCacheService` | `Light.Infrastructure` | `ICacheService` implementation wrapping `Microsoft.Extensions.Caching.Distributed.IDistributedCache`; serializes/deserializes values to/from JSON via `System.Text.Json`. |
| `CacheOptions` | `Light.Infrastructure` | Configuration POCO: `Provider` (`string`, defaults to `"memory"`), `RedisHost`/`RedisPassword` (`string?`). |
| `CacheDataExtensions` *(internal)* | `Light.Infrastructure` | `JsonSerialize<T>` / `ReadFromJson<T>` helpers used internally by `DistributedCacheService`, backed by a shared camelCase `JsonSerializerOptions`. |
| `CacheDeserializationException` | `Light.Exceptions` | Thrown by `Get<T>`/`GetAsync<T>` on a type mismatch (see Notes below). Does **not** derive from `SharedKernel`'s `Light.Exceptions.ExceptionBase` hierarchy — this project has no `ProjectReference` to `SharedKernel`, it just happens to share the namespace. |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddCache(Action<CacheOptions>)` and `AddCache(CacheOptions)` registration helpers. |
| `CacheServiceExtensions` | `Light.Extensions` | `GetOrSetAsync<T>(ICacheService, string, Func<Task<T?>>, TimeSpan? slidingExpiration = null, ...)` — returns the cached value, or invokes the factory, caches, and returns the result on a miss. |

### ⚠️ Namespace layout

This package's `RootNamespace` is the bare `$(BaseNamespace)` (`Light`), not `$(BaseNamespace).Caching` like every other framework package (`Light.Extensions`, `Light.AspNetCore.Swagger`, etc.). As a result, two of its types land in namespaces **already owned by other, unrelated framework packages**, compiled into different assemblies:

- `CacheServiceExtensions` sits in `Light.Extensions` — the same namespace as the `Lightsoft.Extensions` package's ~20 unrelated static helper classes (`StringHelper`, `DateTimeHelper`, `ArgumentChecker`, etc.).
- `CacheDeserializationException` sits in `Light.Exceptions` — the same namespace as `Lightsoft.SharedKernel`'s HTTP-status exception hierarchy (`ExceptionBase`, `NotFoundException`, `ForbiddenException`, ...), despite having no relation to it.

This is a known, intentional trade-off (not a build error — C# allows a namespace to span multiple assemblies), but it means IntelliSense/autocomplete for `Light.Extensions` or `Light.Exceptions` will mix types from unrelated packages once a consumer references both. `ICacheService`/`IAsyncCacheService` also live under `Light.Extensions.Caching` (folder `Extensions/Caching/`) even though they are the package's core contracts, not extension methods.

## How `AddCache` picks a provider

`AddCache(CacheOptions settings)` (the `Action<CacheOptions>` overload just builds a `CacheOptions` and delegates to this one) branches on `settings.Provider`:

- **`"redis"`** — throws a plain `Exception` if `settings.RedisHost` is null/empty. Otherwise builds a `StackExchange.Redis.ConfigurationOptions` (`AbortOnConnectFail = true`, single endpoint = `RedisHost`, `Password` set only if `RedisPassword` is non-empty), calls `services.AddStackExchangeRedisCache(...)` with it, and registers `ICacheService` → `DistributedCacheService` via `AddTransient`.
- **Anything else (including null/empty)** — treated as the default: calls `services.AddMemoryCache()` and registers `ICacheService` → `MemoryCacheService` via `AddTransient`.

Both branches register the `ICacheService` interface — consumers should inject `ICacheService` (or `IAsyncCacheService` if only the async members are needed).

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
using Light.Extensions.Caching; // ICacheService

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

### `GetOrSetAsync`

`Light.Extensions.CacheServiceExtensions` adds a `GetOrSetAsync<T>` helper on `ICacheService`: it returns the cached value, or invokes the factory, stores the result, and returns it on a miss.

```csharp
using Light.Extensions; // same namespace as the Lightsoft.Extensions package — see "Namespace layout" above

var user = await cacheService.GetOrSetAsync(
    $"user:{userId}",
    () => userRepository.GetByIdAsync(userId),
    TimeSpan.FromMinutes(5)); // sliding expiration is optional — omit for no expiration
```

Notes:
- For a value type `T`, a cached value equal to `default(T)` is indistinguishable from a cache miss (same limitation as `Get<T>`), so the factory runs again in that case.
- If the factory returns `null` (e.g. a repository lookup that found nothing), that `null` is returned as-is and is **not** cached — the next call invokes the factory again rather than caching the negative result.

## Notes

- `Try*` methods (`TryGet`, `TrySet`, `TryGetAsync`, `TrySetAsync`) catch all exceptions, log via the injected `ILogger<MemoryCacheService>`/`ILogger<DistributedCacheService>` (`LogError` with the key and exception message), and return `default`/complete silently. The plain `Get`/`Set`/`GetAsync`/`SetAsync` methods do **not** catch anything and propagate exceptions to the caller.
- A missing key generally does not throw on its own: `MemoryCacheService.Get<T>` delegates to `Microsoft.Extensions.Caching.Memory.IMemoryCache.Get<T>`, which returns `default(T)` for an absent key, and `DistributedCacheService.Get<T>` treats a null/empty `GetString` result as `default(T)` before attempting JSON deserialization (`CacheDataExtensions.ReadFromJson<T>`). The `Try*` variants mainly guard against other failures — e.g. a stored value that doesn't match the requested `T`, or the underlying cache provider (e.g. Redis connection) throwing. `Get<T>`/`TryGet<T>`/`GetAsync<T>`/`TryGetAsync<T>` all return `T?` to reflect this.
- A stored value that doesn't match the requested `T` throws `Light.Exceptions.CacheDeserializationException` (wrapping the provider-specific cause — `InvalidCastException` for `MemoryCacheService`, `JsonException` for `DistributedCacheService` — as `InnerException`) from both `Get<T>`/`GetAsync<T>` implementations, so code written against `ICacheService` can catch one exception type regardless of the backing provider. `TryGet<T>`/`TryGetAsync<T>` swallow it like any other failure and return `default`.
- Only sliding expiration is exposed (`Set<T>(key, value, slidingExpiration)` → `MemoryCacheEntryOptions.SlidingExpiration` or `DistributedCacheEntryOptions.SetSlidingExpiration`; omit/pass `null` for no expiration). There is no parameter for absolute expiration.
- `AddCache` registers `ICacheService` with `AddTransient`, so a new `MemoryCacheService`/`DistributedCacheService` wrapper is created per resolution — cheap, since the wrapped `Microsoft.Extensions.Caching.*` cache itself is registered as a singleton by `AddMemoryCache`/`AddStackExchangeRedisCache`.
- `AddCache` throws a plain `System.Exception` (not a more specific exception type) when `Provider == "redis"` and `RedisHost` is missing — catch `Exception` broadly if you need to handle this at startup, or validate `CacheOptions` yourself beforehand.
- The project has `<Nullable>enable</Nullable>` (inherited from `Directory.Build.props`).
