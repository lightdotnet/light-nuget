# TODO — Framework

Open follow-ups from a full review of the `Framework` solution (`Framework.slnx`), originally covering 9 `src/`
projects (Authorization, Extensions, Identity, Identity.EntityFrameworkCore, Modularity, OpenApi, SharedKernel,
Swagger, WebHost) plus the samples and tests. `src/Http` and `src/OpenApi` have since been removed from the repo;
see the **Removed projects** note below. Findings came from a dependency-graph pass, a code-quality pass, and a
performance pass. Nothing below has been fixed yet except SharedKernel — this is a tracking list for later
implementation.

---

## Open items

### Blocks the build

- [ ] **`Framework.slnx` and `Sample.AspNetCore.csproj` still reference deleted Identity projects.**
  `Framework.slnx`; `samples/Sample.AspNetCore/Sample.AspNetCore.csproj`
  `src/Identity` and `src/Identity.EntityFrameworkCore` were removed by commit `f31f0aa "feat: remove identity
  framework"`, but the `.slnx` still lists both `<Project>` entries and `Sample.AspNetCore.csproj` still has a
  `<ProjectReference>` to the deleted `Identity.EntityFrameworkCore.csproj`. `dotnet build`/`dotnet sln` on this
  solution fails as-is. `README.md` also still lists Identity as a package. Fix: remove the stale entries/reference
  and update `README.md`, or restore the projects if removal was unintentional.

- [ ] **`Sample.OpenApi.csproj` still references the now fully-deleted `OpenApi` project family.**
  `samples/Sample.OpenApi/Sample.OpenApi.csproj`
  Was already broken before the removal (pointed at `../../src/AspNetCore.OpenApi/AspNetCore.OpenApi.csproj`, a
  path that never existed — the real folder was `src/OpenApi`). Now that `src/OpenApi` has been deleted entirely,
  there is nothing left to point this at. The project also still isn't included in `Framework.slnx`. Recommend
  deleting `samples/Sample.OpenApi/` outright unless a replacement OpenApi sample is planned.

### Correctness bugs

- [ ] **`ArgumentChecker` default-value guard never fires for value types.**
  `src/Extensions/ArgumentChecker.cs:52`
  `EqualityComparer<T>.Default.Equals(input)` binds to `object.Equals(object)` (no matching single-arg overload),
  not a default-value comparison. The XML doc promises throwing on null-or-default, but only the null case ever
  throws. Fix: `EqualityComparer<T>.Default.Equals(input, default!)`. Public API — the fix makes the guard *more*
  strict, flag to team since callers may currently rely on the broken permissive behavior.

- [ ] **`DateTimeHelper.IsNearlyIn*` returns true for any past date.**
  `src/Extensions/DateTimeHelper.cs:13-53`
  `diff = dateTime - DateTime.Now` is negative for any past `dateTime`, so `diff.TotalMinutes <= mins` is trivially
  true regardless of how far in the past. Missing lower-bound check. Also uses `DateTime.Now` (local time) instead
  of `UtcNow`, fragile for a shared framework. Fix: `return diff.TotalMinutes >= 0 && diff.TotalMinutes <= mins;`
  (or `Math.Abs(...)` if "nearly" should mean either direction).

- [ ] **`OrderedConverterBase` only strips one of two ordering converter factories — possible `StackOverflowException`.**
  `src/WebHost/Extensions/Json/OrderedConverterBase.cs:14-21`
  Constructor strips `BaseFirstOrderedConverterFactory` from the cloned `_safeOptions` but not
  `PropertyOrderedConverterFactory` (`src/WebHost/Extensions/Json/PropertyOrderedConverter.cs:8`). For a
  self-referencing type (or nested property of the same `T`) decorated with `[PropertyOrder]`,
  `PropertyOrderedConverter<T>.Write/Read` can recurse into itself indefinitely. Fix: strip both converter
  factories (or all "ordered" family factories) in the base constructor.

- [ ] **`LoadConfigurationFrom(string[])` combines paths instead of loading each one.**
  `src/WebHost/AspNetCore/Builder/JsonConfigurationLocation.cs:52-53`
  `Path.Combine(paths)` treats the array as nested path segments (`["cfgA","cfgB"]` → `cfgA\cfgB`), not as multiple
  independent config sources. Fix: iterate and chain, e.g. `foreach (var p in paths) host =
  host.LoadConfigurationFrom(p); return host;`.

- [ ] **`??=` inside a redundant null-or-empty check never fires for empty (non-null) input.**
  `src/Extensions/ArgumentChecker.cs:54-57`
  Pattern is `if (x == null || x.IsEmpty) { x ??= fallback; }` — `??=` only assigns on null, so a non-null empty
  string skips the fallback silently. Fix: use a plain assignment inside the `if`, not `??=`. (The equivalent bug
  in `src/Modularity/Extensions/AssemblyTypeExtensions.cs` has been fixed — see Modularity section below.)

- [ ] **`ClaimExtensions.FindFirstValue` is dead code — shadowed by the BCL's own method.**
  `src/Extensions/ClaimExtensions.cs:19-22`
  `ClaimsPrincipal.FindFirstValue(string)` has existed as a BCL instance method since .NET Core; C# always prefers
  an applicable instance method over an extension with the same signature, so this extension is unreachable — its
  custom null-check/throw behavior never runs. Remove it, or rename if the custom behavior is actually wanted.

- [ ] **`RequestLoggingMiddleware` swallows downstream exceptions when `IncludeResponse` is enabled.**
  `src/WebHost/AspNetCore/Middlewares/RequestLoggingMiddleware.cs:120-162`
  `await next(context)` sits inside a `try` whose `catch (Exception ex)` only logs `ex.Message` without rethrowing.
  Any exception from downstream middleware/handlers (including ones meant for the exception-handling pipeline) is
  silently swallowed instead of propagating. Fix: only wrap the post-`next` body-copy logic in try/catch, or
  `catch { logger.LogError(...); throw; }`.

- [ ] **`PermissionPolicyProvider` breaks all non-permission named policies.**
  `src/Authorization/AspNetCore/Authorization/PermissionPolicyProvider.cs:22`
  `GetPolicyAsync` returns `null` for any policy name it doesn't recognize as permission-shaped, instead of falling
  back to `FallbackPolicyProvider.GetPolicyAsync(policyName)` — that call is present but commented out. Any policy
  registered normally via `AddAuthorization(options => options.AddPolicy(...))` stops resolving once this provider
  is registered. Confirm intent; likely needs the fallback restored.

### Performance

- [ ] **Ordered JSON converters do uncached reflection on every `Write()` call.**
  `src/WebHost/Extensions/Json/OrderedConverterBase.cs:33`; `BaseFirstOrderedConverter.cs:31-52`;
  `PropertyOrderedConverter.cs:29-38`
  Full `GetProperties()` + attribute lookup + `OrderBy` + list allocation runs per serialize call, not once per
  type. Registered globally by `AddDefaultJsonOptions()` (`MvcBuilderExtensions.cs:52`), so every consumer using it
  pays this on every JSON response. Since STJ caches the converter instance per type, compute the ordered property
  list once in the constructor (or via `Lazy<T>`/static cache). Highest-impact perf finding in this review.

- [ ] **`PermissionPolicyProvider` rebuilds the authorization policy on every request.**
  `src/Authorization/AspNetCore/Authorization/PermissionPolicyProvider.cs:13-24`
  ASP.NET Core does not cache policies resolved through a custom `IAuthorizationPolicyProvider`, so every request
  to a permission-policy-protected endpoint rebuilds a new `AuthorizationPolicyBuilder`/`PermissionRequirement`.
  Fix: cache built `AuthorizationPolicy` instances in a `ConcurrentDictionary<string, AuthorizationPolicy>` keyed
  by policy name.

- [ ] **`RequestLoggingMiddleware.CheckSkipWriteLog` allocates per request.**
  `src/WebHost/AspNetCore/Middlewares/RequestLoggingMiddleware.cs:39-47`
  New `List<string>` + `AddRange` per call, plus `httpRequest.Path.ToString()` called once per exclude-path entry
  inside the `Any` lambda instead of once. Precompute the merged exclude list once; hoist the `ToString()` call.

- [ ] **`RequestLoggingMiddleware.WriteResponseAsync` unconditionally buffers the full response body + sync read.**
  `src/WebHost/AspNetCore/Middlewares/RequestLoggingMiddleware.cs:120-162`
  When `IncludeResponse` is enabled, the entire response is buffered into an uncapped `MemoryStream`, then read via
  synchronous `StreamReader.ReadToEnd()` instead of `ReadToEndAsync()`. Impact scales with response size (file
  downloads, large payloads). Consider `ReadToEndAsync` + a size threshold/opt-out.

- [ ] **`ExceptionHandlerExtensions` allocates a new `JsonSerializerOptions` per exception.**
  `src/WebHost/AspNetCore/ExceptionHandlers/ExceptionHandlerExtensions.cs:100-104`
  Low frequency (exception path) but easy fix: cache as `static readonly`.

- [ ] **`ExceptionHandlerExtensions` double-enumerates `ValidationErrors`.**
  `src/WebHost/AspNetCore/ExceptionHandlers/ExceptionHandlerExtensions.cs:47-59`
  `.Any()` then `string.Join` over the same `Select(...)` projection. Use `.Count > 0` (it's a dictionary) and
  materialize the projection once.

- [ ] **`LowercaseControllerNameConvention.Convert` builds a string with `+=` in a loop (O(n²)).**
  `src/WebHost/AspNetCore/Mvc/ControllerExtensions.cs:19-32`
  Use `StringBuilder`.

- [ ] **`ValueObject.GetHashCode` throws on empty equality components and uses weak XOR combining.**
  `src/SharedKernel/Domain/ValueObjects/ValueObject.cs:33-38`
  `.Select(...).Aggregate((x, y) => x ^ y)` throws `InvalidOperationException` for a zero-element sequence and XOR
  is collision-prone. Prefer `HashCode.Combine`/the `HashCode` accumulator struct, seeded so empty doesn't throw.

- [ ] **`RandomHelper` allocates a `new Random()` per call.**
  `src/Extensions/RandomHelper.cs:17`
  Use `Random.Shared.Next(...)` (available since .NET 6) instead.

- [ ] **`DataConverter` double-enumerates and reflection-per-call without caching (needs profiling to confirm impact).**
  `src/Extensions/DataConverter.cs:39,50` (`values != null && values.Any()` then `string.Join` re-enumerates);
  `src/Extensions/ValueHandler.cs:15-19,36-37,59-63` (`GetType().GetProperties()` + LINQ `Where`, no per-type
  cache, boxing on value types via `PropertyInfo.GetValue/SetValue`)
  `src/SharedKernel/Extensions/DynamicObject/DynamicMapper.cs:14,35-64` (uncached `GetProperty` lookup per column,
  plus a `catch { }` that silently swallows all conversion errors — correctness smell as much as perf)
  Only worth fixing if these run per-row/per-request in practice; cache `PropertyInfo` lookups
  (`ConcurrentDictionary<(Type,string), PropertyInfo>`) if profiling shows it matters.

### SharedKernel — fixed

- [x] **`ValueObject.GetHashCode` throws on empty equality components + weak XOR combining.**
  `src/SharedKernel/Domain/ValueObjects/ValueObject.cs:33-38` — now uses the `HashCode` accumulator struct
  (`HashCode.Add`/`ToHashCode()`), which doesn't throw on an empty sequence and combines better than XOR.

- [x] **`DynamicMapper` uncached reflection + silently swallowed conversion errors.**
  `src/SharedKernel/Extensions/DynamicObject/DynamicMapper.cs` — property lookups are now cached per type in a
  `ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>`; the blanket `catch { }` around conversion was
  removed so conversion failures now surface instead of being silently dropped (intentional behavior change — flag
  if any consumer relied on the old silent-skip behavior).

- [x] **`IAggregateRoot.cs` block-scoped namespace.**
  `src/SharedKernel/Contracts/IAggregateRoot.cs` — converted to file-scoped namespace, consistent with the rest of
  the codebase.

- [x] **`LightId` struct with no fields.**
  `src/SharedKernel/Domain/LightId.cs` — converted `struct LightId` to `static class LightId`. Verified no
  consumer in this solution does `new LightId()` or uses it as a generic constraint (only `LightId.NewId()` call
  sites exist), so this breaking-in-theory change is safe here; downstream consumers outside this repo should still
  be notified via release notes since it's a public API shape change.

- [x] **`ValidationException.errorMsg` const casing.**
  `src/SharedKernel/Exceptions/ValidationException.cs:7` — renamed to `ErrorMessage` (PascalCase, private member,
  no external impact).

### Modularity — fixed

- [x] **`AssemblyTypeExtensions` (formerly `AsemblyTypeExtensions`) `??=` bug.**
  `src/Modularity/Extensions/AssemblyTypeExtensions.cs:9-13` — replaced `assemblies ??= AppDomain.CurrentDomain
  .GetAssemblies();` (never fired for a non-null empty array) with a plain assignment, so
  `ModuleBuilderExtensions.UseModules(builder, [])` now correctly falls back to scanning all loaded assemblies.

- [x] **`AsemblyTypeExtensions` misspelling.**
  File and class renamed to `AssemblyTypeExtensions` (internal class, no consumer impact). All 3 call sites
  (`ModuleBuilderExtensions.cs` x2, `ModuleServiceCollectionExtensions.cs` x1) updated.

- [x] **`AutoAddServiceExtensions` scanned all assemblies 3 times instead of once.**
  `src/Modularity/Extensions/DependencyInjection/AutoAddServiceExtensions.cs` — `AutoAddDependencies()` now scans
  `AppDomain.CurrentDomain.GetAssemblies()` once into a materialized list and reuses it for all three lifetime
  buckets (transient/scoped/singleton), instead of `AddServices` re-scanning per lifetime.

- [x] **Non-deterministic interface-matching heuristic in DI auto-registration.**
  `src/Modularity/Extensions/DependencyInjection/AutoAddServiceExtensions.cs` — interface matching changed from
  `x.Name.Contains(s.Name)` (could match multiple candidate interfaces non-deterministically via `FirstOrDefault`
  over an unordered `GetInterfaces()`) to an exact `x.Name == "I" + s.Name` convention match. A class without an
  interface literally named `I<ClassName>` is now registered by concrete type only (no interface mapping) —
  documented as a behavior change in the new Modularity README.

### Naming — non-breaking (internal/private, safe to rename)

- [ ] **`ControllerExtensions.cs` file name doesn't match its contents.**
  `src/WebHost/AspNetCore/Mvc/ControllerExtensions.cs` contains `LowercaseControllerNameConvention` and
  `OrderedPropertiesJsonTypeInfoResolver`, neither of which is an extension method. Split into files named after
  the types, or rename to something like `MvcConventions.cs`.

- [ ] **Private consts using camelCase instead of PascalCase.**
  `src/SharedKernel/Exceptions/ValidationException.cs:7` (`errorMsg`); `src/Extensions/DataConverter.cs:9`
  (`_defaultSplitChar`). Inconsistent with typical C# const conventions.

### Naming — breaking (public API, needs version bump / explicit sign-off)

- [ ] **`ExceptionHandlerOptions.HideUndentifyException` misspelled.**
  `src/WebHost/AspNetCore/ExceptionHandlers/ExceptionHandlerOptions.cs:5` → `HideUnidentifiedException` (or
  `HideUnhandledExceptionDetails`). Public settable property, bound from config.

- [ ] **`Swagger.Startup` class name too generic, inconsistent with the rest of the solution.**
  `src/Swagger/Startup.cs` — every other DI-extension class here is named `*ServiceCollectionExtensions`/
  `*ApplicationBuilderExtensions` (e.g. `JwtAuthServiceCollectionExtensions`,
  `ExceptionHandlerServiceCollectionExtensions`). `Startup` also collides conceptually with ASP.NET's own
  convention. Split into `SwaggerServiceCollectionExtensions` (`AddSwagger`) and
  `SwaggerApplicationBuilderExtensions` (`UseSwagger`).

- [ ] **`DataConverter.ToInt64FromBinary` name doesn't match behavior.**
  `src/Extensions/DataConverter.cs:120` — defaults `toBase = 16` and accepts any base, but the name says "binary"
  (base 2). Rename to `ToInt64(this string value, int fromBase = 16)`.

- [ ] **`DataConverter.AreaTextToArray`/`AreaTextToList` use unexplained jargon.**
  `src/Extensions/DataConverter.cs:58,69` — "AreaText" presumably means HTML textarea content. Rename to
  `SplitLines`/`ToLines`.

- [ ] **`RandomHelper.String`/`RandomHelper.Number` names are ambiguous out of context.**
  `src/Extensions/RandomHelper.cs:26,37` — shadow common type/primitive names and don't convey "random" when read
  via `using static`. Rename to `GenerateString`/`GenerateNumber` (or `NextString`/`NextNumber` to mirror
  `Random.Next`).

- [ ] **`ApiControllerBase.Ok()` shadows `ControllerBase.Ok()` via `new` with different behavior.**
  `src/WebHost/AspNetCore/Mvc/ApiControllerBase.cs:19` — returns a wrapped `Result` envelope instead of a plain 200
  body. Polymorphism breaks if called through a `ControllerBase` reference, and the name gives no hint the response
  shape differs. Consider a distinctly-named method (e.g. `Success()`) instead of shadowing the base method name.

- [ ] **`DataConverter.ToList` return type inconsistent across branches.**
  `src/Extensions/DataConverter.cs:25` — declared `IEnumerable<string>`; empty branch returns a `List<string>`,
  non-empty branch returns a bare `string[]`. Materialize consistently (e.g. `.ToList()` in both branches);
  signature itself doesn't need to change.

- [ ] **`ObjectHelper.IsList` only matches concrete `List<T>`, not other `IList` implementors.**
  `src/Extensions/ObjectHelper.cs:78` — generic name/doc suggest broader `IList` detection than the actual
  equality-style check performs (arrays, `Collection<T>`, etc. don't match). Either rename to
  `IsListOfT`/`IsGenericList` to reflect actual behavior, or change the check if broader detection was intended.

### Dead code / minor

- [ ] **Commented-out fallback in `PermissionPolicyProvider`** — see correctness section above; remove the dead
  comment once resolved.

- [ ] **Commented-out `[Obsolete]` on `UseLightExceptionHandler`.**
  `src/WebHost/AspNetCore/Builder/MiddlewareApplicationBuilderExtensions.cs:37` — either apply the attribute for
  real or remove the comment. Also: two parallel exception-handling mechanisms now coexist
  (`ExceptionHandlerMiddleware` via `UseLightExceptionHandler`, and `ExceptionHandler`/`IExceptionHandler` via
  `AddGlobalExceptionHandler`), both funneling into `HandleExceptionAsync` — confirm which one should be the
  documented/supported path going forward.

- [ ] **`TitleFilter` is dead code — defined but never registered.**
  `src/Swagger/TitleFilter.cs` — usage is commented out. Remove or wire it up.

- [ ] **Unused `contentType` variable in `RequestLoggingMiddleware`.**
  `src/WebHost/AspNetCore/Middlewares/RequestLoggingMiddleware.cs:65` — computed via
  `context.Response.Headers.ContentType.ToString()` but never used in the log line, wasted work + misleading.
  Also a stray commented-out `requestHost` variable at line 61.

- [ ] **Wrong `paramName` passed to `ArgumentNullException.ThrowIfNull`.**
  `src/Swagger/Startup.cs:21` — passes `nameof(SwaggerOptions)` (the type name) instead of `nameof(settings)` (the
  parameter), producing a misleading exception message.

- [ ] **JWT secret encoded as ASCII instead of UTF8.**
  `src/WebHost/Extensions/DependencyInjection/JwtAuthServiceCollectionExtensions.cs:16` —
  `Encoding.ASCII.GetBytes(secretKey)` silently corrupts non-ASCII characters in the secret. Use
  `Encoding.UTF8.GetBytes(secretKey)`. Also `bearer.RequireHttpsMetadata = false;` is hardcoded rather than
  configurable — worth a closer security pass.

- [ ] **Block-scoped namespace inconsistent with rest of codebase.**
  `src/SharedKernel/Contracts/IAggregateRoot.cs` uses `namespace Light.Contracts { ... }` while virtually every
  other file in this solution uses file-scoped namespaces.

---

## Removed projects

- **`src/Http` and `src/OpenApi` have been deleted from the repo.** `Http.csproj` and `OpenApi.csproj` are gone;
  neither ever caused a solution-load failure on their own (`Http` was never listed in `Framework.slnx`), but
  `OpenApi` was — its `<Project>` entry has been removed from `Framework.slnx` as part of this sync so the solution
  loads/builds cleanly again. The dangling reference from `samples/Sample.OpenApi/Sample.OpenApi.csproj` to the old
  `src/OpenApi` project family is now doubly orphaned — see the "Blocks the build" item above.
- The `Asp.Versioning.Mvc.ApiExplorer` exact-vs-floating version-mismatch previously noted between `OpenApi.csproj`
  and `Swagger.csproj`/`WebHost.csproj` no longer applies — `OpenApi.csproj` is gone.

## Dependency graph notes (informational, not action items)

- Graph is nearly flat: only real internal project reference is `WebHost → SharedKernel`; Authorization, Extensions,
  Modularity, Swagger are all leaves with no internal `ProjectReference`. No circular references.
- `Directory.Build.props` centralizes `AspnetVersion=10.*`, `TargetFramework=net10.0`, `Nullable=enable`,
  `ImplicitUsings=enable`, SourceLink. `Extensions.csproj` intentionally overrides to `netstandard2.1` (broad
  compat). `Sample.OpenApi.csproj` overrides to `net9.0` — moot if that sample is removed per the item above.
