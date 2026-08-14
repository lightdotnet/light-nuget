[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Caching.Tests

NUnit test project for the `Lightsoft.Caching` package, part of the `Framework` solution (`src/framework`, `Framework.slnx`). Targets **net10.0** (see `TargetFramework` in `Caching.Tests.csproj`; stale `obj/Debug/net9.0` build output folders exist on disk but are not what the project currently targets).

Uses `NUnit` 4.6.1 + `NUnit3TestAdapter` 6.2.0 + `Microsoft.NET.Test.Sdk` 18.6.0, with `NUnit.Analyzers` and `coverlet.collector` for coverage collection. `GlobalUsings.cs` globally imports `NUnit.Framework`.

## What's referenced

`Caching.Tests.csproj` has a single `ProjectReference`: `Caching`. It also explicitly references `Microsoft.Extensions.Configuration`/`Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.DependencyInjection` (otherwise only pulled in transitively).

## What's covered

### `LightAssert.cs`

A small internal extension-method helper (`namespace Caching.Tests`) wrapping common `NUnit.Framework.Assert.That` calls for more fluent/readable call sites:
- `ShouldBe<T>(this T value, T equalTo)` → `Assert.That(value, Is.EqualTo(equalTo))`
- `ShouldNotBeNullOrEmpty<T>(this T value)` → `Assert.That(value, Is.Not.Null.And.Not.Empty)`
- `ShouldContains<T>(this IEnumerable<T> value, object obj)` → `Assert.That(value, Contains.Item(obj))`
- `ShouldBeTrue(this bool value)` / `ShouldBeFalse(this bool value)` → thin wrappers over `ShouldBe(value, true/false)`

It adds no behavior beyond NUnit's built-in constraint model — it's purely fluent sugar, used throughout `CachingTests/`.

### `CachingTests/`

`MemoryCacheServiceTests` and `DistributedCacheServiceTests` construct the real service classes against real in-process backing stores — `Microsoft.Extensions.Caching.Memory.MemoryCache` and `Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache` respectively — so no mocking and no live Redis is needed. Covers Get/Set round-trips (sync and async), missing-key defaults, sliding-expiration actually expiring, `Remove`/`RemoveAsync` actually removing, `Get` throwing on a type mismatch vs. `TryGet` swallowing it, and `Set` throwing on a null key vs. `TrySet` swallowing it.

`CacheServiceExtensionsTests` covers `CacheServiceExtensions.GetOrSetAsync` — cache hit returns the cached value without invoking the factory, a miss invokes the factory and caches the result, and a factory returning `null` is returned as-is without being cached.

## Running the tests

From this folder:

```powershell
dotnet test
```

Or from the solution root (`src/framework`):

```powershell
dotnet test Framework.slnx
```
