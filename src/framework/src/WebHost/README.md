[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.AspNetCore.Extensions

NuGet package ID: **`Lightsoft.AspNetCore.Extensions`** (see `WebHost.csproj`; the `.csproj` file itself
is still named `WebHost.csproj` and the project folder is `WebHost`, but the assembly/package name is
`Lightsoft.AspNetCore.Extensions`). Version `10.1` at the time of writing.

This is the largest and most complex project in the `Framework` solution (`src/framework/Framework.slnx`),
and the only one with an internal `ProjectReference` to another project in the solution
(`..\SharedKernel\SharedKernel.csproj`). It also depends on `Asp.Versioning.Mvc.ApiExplorer`,
`Lightsoft.Result` (external package), and `Microsoft.AspNetCore.Authentication.JwtBearer`.

Per the [main README](https://github.com/lightdotnet/light-nuget#readme), this is the "Default Hosting config"
package: JWT auth setup, request logging middleware, exception handling, JSON ordering converters, MVC
conventions/base controllers, and config-file loading helpers for a typical ASP.NET Core host.

## JWT authentication

Namespace: `Light.Extensions.DependencyInjection` (`JwtAuthServiceCollectionExtensions`).

Two overloads of `AddJwtAuth`:

- `AddJwtAuth(string issuer, string secretKey, JwtBearerEvents jwtBearerEvents, string roleClaimType)` —
  low-level overload; wires up `AddAuthentication`/`AddJwtBearer` with `ValidateIssuerSigningKey = true`,
  `ValidIssuer = issuer`, `ValidateAudience = false`, `RoleClaimType = roleClaimType`, `ClockSkew =
  TimeSpan.Zero`, and the `JwtBearerEvents` you pass in verbatim.
- `AddJwtAuth(string issuer, string secretKey, string roleClaimType, string signalRHub = "/signalr-hub")`
  — convenience overload that builds a default `JwtBearerEvents` and delegates to the overload above:
  - `OnChallenge` calls `context.HandleResponse()` and, if the response hasn't started, throws
    `UnauthorizedException("Authentication Failed.")` (from `Light.Exceptions`, in `SharedKernel`).
  - `OnForbidden` throws `ForbiddenException("You are not authorized to access this resource.")`.
  - `OnMessageReceived` reads the `access_token` query-string value into `context.Token` when the request
    path starts with `signalRHub` (default `/signalr-hub`) — for SignalR clients, which can't set an
    `Authorization` header on the WebSocket handshake.

```csharp
builder.Services.AddJwtAuth(issuer: "my-issuer", secretKey: "...", roleClaimType: ClaimTypes.Role);
```

Both overloads set `RequireHttpsMetadata = false` and `SaveToken = true` unconditionally.

## Configuration loading

Namespace: `Light.AspNetCore.Builder` (`JsonConfigurationLocation`).

- `LoadConfigurationFrom(this IHostApplicationBuilder host, string? path)` — if `path` is null/empty,
  returns `host` unchanged (default config). Otherwise, loads every `*.json` file in `path` whose name
  doesn't start with `appsettings` (each with `optional: false, reloadOnChange: true`), then loads
  `appsettings.json` (required) and `appsettings.{EnvironmentName}.json` (optional) from the working
  directory, then adds environment variables. Files from `path` are added *before* `appsettings.json`, so
  root `appsettings*.json` values win on conflicts.
- `LoadConfigurationFrom(this IHostApplicationBuilder host, string[]? paths)` — iterates `paths` and calls
  the single-`path` overload **once per entry, in sequence**, re-assigning `host` each time. Each path is
  loaded independently (its own `*.json` scan plus its own `appsettings.json`/`appsettings.{env}.json`/env-var
  reload); paths are not combined into one nested lookup path.

```csharp
builder.LoadConfigurationFrom(new[] { "config/shared", "config/local" });
```

## Request logging

Namespace: `Light.AspNetCore.Middlewares` (options + middleware), registered via
`MiddlewareApplicationBuilderExtensions.UseLightRequestLogging()` in `Light.AspNetCore.Builder`.

`RequestLoggingOptions`:

| Property | Type | Purpose |
|---|---|---|
| `Enable` | `bool` | `UseLightRequestLogging()` is a no-op unless this is `true`. |
| `IncludeRequest` | `bool` | Logs the (minified, if JSON) request body. |
| `IncludeResponse` | `bool` | Captures and logs the response body via a buffering `MemoryStream`. |
| `ExcludePaths` | `List<string>?` | Additional path substrings to skip logging for. |

`RequestLoggingMiddleware` always merges `"hangfire"` and `"swagger"` into the exclude list in addition to
whatever `ExcludePaths` you configure — a request is skipped if its path *contains* any excluded substring.
Every non-excluded request is timed with a `Stopwatch` and logged with method, status code, path, query,
scheme, client IP, and `HttpContext.TraceIdentifier`.

```csharp
app.UseLightRequestLogging(); // no-op unless RequestLoggingOptions.Enable == true
```

## Exception handling

Two parallel registration mechanisms exist side by side; both ultimately call the same
`HandleExceptionAsync` logic in the internal `ExceptionHandlerExtensions` (`Light.AspNetCore.ExceptionHandlers`).
Pick one — they are not meant to be combined:

- **Middleware-based (legacy):** `MiddlewareApplicationBuilderExtensions.UseLightExceptionHandler()` (in
  `Light.AspNetCore.Builder`) registers `ExceptionHandlerMiddleware`, a `try/catch` around `next(context)`.
- **`IExceptionHandler`-based (current ASP.NET Core mechanism):**
  `ExceptionHandlerServiceCollectionExtensions.AddGlobalExceptionHandler()` (in
  `Light.Extensions.DependencyInjection`) registers the `ExceptionHandler` class via
  `services.AddExceptionHandler<ExceptionHandler>()` plus `services.AddProblemDetails()`. Requires
  `app.UseExceptionHandler()` in the pipeline (standard ASP.NET Core call, not provided by this package).

`HandleExceptionAsync` behavior:

- Skips writing anything for a Hangfire-dashboard "response already started" error (path contains
  `"hangfire"` and message contains `"StatusCode cannot be set because the response has already started."`).
- Unwraps to the innermost `InnerException` for any exception that isn't a `Light.Exceptions.ExceptionBase`.
- Maps `ValidationException` → its `StatusCode`, joining `ValidationErrors` into a `key: v1,v2|...` message;
  any other `ExceptionBase` → its own `StatusCode`; `KeyNotFoundException` → 404; anything else → 500, with
  the message replaced by `"Internal Server Error"` when `ExceptionHandlerOptions.HideUnidentifiedException`
  is `true` (default `true`).
- Logs the exception (source type + message) and, if the response hasn't started, writes a
  `Light.Contracts.Result` JSON body (camelCase, nulls omitted) containing `Code`, `Message`, and
  `RequestId` (= `TraceIdentifier`).

`ExceptionHandlerOptions` is bound the normal `IOptions<T>` way; register/configure it via your own
`services.Configure<ExceptionHandlerOptions>(...)` before calling either registration mechanism.

## JSON ordering converters

Namespace: `Light.Extensions.Json`. Registered via `MvcBuilderExtensions.AddDefaultJsonOptions()`
(`Light.Extensions.DependencyInjection`), which also sets `ReferenceHandler.IgnoreCycles`, camelCase naming,
case-insensitive property matching, and a `JsonStringEnumConverter`.

- **`BaseFirstOrderedConverterFactory` / `BaseFirstOrderedConverter<T>`** — the only converter currently
  registered by `AddDefaultJsonOptions()`. Orders a type's JSON properties base-class-first (walks the
  inheritance chain from `object` down to `T`, declared-only properties per level), then by the optional
  `[JsonPropertyOrder]` attribute within each level. Applies to any non-string class that isn't
  `IEnumerable` (so POCOs, not collections/dictionaries).
- **`PropertyOrderedConverterFactory` / `PropertyOrderedConverter<T>`** — orders properties purely by a
  `[PropertyOrder]` attribute (`Light.Contracts`); applies only to concrete classes that have at least one
  property decorated with `[PropertyOrder]`. **Not currently wired into `AddDefaultJsonOptions()`** — it
  exists in the package but must be added to `JsonSerializerOptions.Converters` manually if needed.

Both derive from the shared abstract `OrderedConverterBase<T>`, which:

- clones the incoming `JsonSerializerOptions` once per converter instance and strips **both**
  `BaseFirstOrderedConverterFactory` and `PropertyOrderedConverterFactory` from the clone, so nested/self-
  referencing properties of the same ordered type don't recurse back into the ordering logic infinitely.
- caches the computed, ordered `PropertyInfo[]` the first time `Write()` runs (`GetPropertyInfos()` is
  only reflected once per converter instance, not once per serialize call — System.Text.Json creates one
  converter instance per type and reuses it).

```csharp
services.AddControllers().AddDefaultJsonOptions();
```

Also in `Light.AspNetCore.Mvc`: `OrderedPropertiesJsonTypeInfoResolver`, a
`DefaultJsonTypeInfoResolver` override that assigns `JsonPropertyInfo.Order` alphabetically by property
name for every object type — a different (contract-resolver-based) ordering strategy from the two
converters above, not wired together with them by default.

## MVC base controllers and conventions

Namespace: `Light.AspNetCore.Mvc`.

- **`ApiControllerBase`** — abstract `[ApiController]` with route `api/[controller]`. Exposes
  `Success()` and `Success<T>(T data)`, both `[ApiExplorerSettings(IgnoreApi = true)]`, wrapping the result
  in the framework's `Result`/`Result<T>` envelope (`Light.Contracts`), stamping `RequestId` with
  `HttpContext.TraceIdentifier`, and converting to an `IActionResult` via `ActionResultExtensions.ToActionResult()`
  (status code taken from `result.ToHttpStatusCode()`).
- **`VersionedApiController`** — abstract, derives from `ApiControllerBase`, overrides the route to
  `api/v{version:apiVersion}/[controller]` and applies `[ApiVersion("1.0")]` (uses `Asp.Versioning`).
- **`LowercaseControllerNameConvention`** — internal `IControllerModelConvention`; lowercases a
  controller's name and inserts a separator (default `"_"`) before each uppercase letter after the first
  (e.g. `OrderItems` → `order_items`). Registered via `MvcBuilderExtensions.AddLowercaseControllers()`
  (either the no-argument overload using the default convention, or an overload accepting a custom
  `IControllerModelConvention`).
- **`AddInvalidModelStateHandler()`** (`MvcBuilderExtensions`) — replaces the default 400 response for
  invalid model state with a `Light.Contracts.Result` (`Code = ResultCode.BadRequest`), joining field
  errors into a `Model_prop: err1,err2|...` message.

```csharp
public class OrdersController : ApiControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(Guid id) => Success(order);
}
```

## Trace ID middleware

Namespace: `Light.AspNetCore.Middlewares`, registered via `UseGuidTraceId()` / `UseGuidV7TraceId()`
(`Light.AspNetCore.Builder`). Both set `HttpContext.TraceIdentifier` early in the pipeline and echo it back
as the `X-Trace-Id` response header:

- `GuidTraceIdMiddleware` — `TraceIdentifier = Guid.NewGuid().ToString()` (random GUID v4).
- `GuidV7TraceIdMiddleware` — `TraceIdentifier = LightId.NewId()` (GUID v7, from `Light.Domain` in
  `SharedKernel` — time-ordered, so trace IDs sort chronologically).

Use at most one of the two.

## API versioning

Namespace: `Light.Extensions.DependencyInjection` (`ApiVersionServiceCollectionExtensions`).
`AddApiVersion(int version, int minorVersion = 0, bool groupByNameAndVersion = true)` calls
`AddApiVersioning` (default version = `version.minorVersion`, `AssumeDefaultVersionWhenUnspecified = true`,
`ReportApiVersions = true`) followed by `AddApiExplorer` (`GroupNameFormat = "'v'VVV"`,
`SubstituteApiVersionInUrl = true`, and a group-name formatter that appends `_{apiVersion}` to the group
name when `groupByNameAndVersion` is `true`).

## Other utilities

- **`Light.AspNetCore.Authorization.BasicAuthorizationExtensions.ReadBasicAuthorization()`** — reads the
  `Authorization` header, strips a `"Basic "` prefix, base64-decodes with ISO-8859-1, and returns the raw
  `"username:password"` string (no further parsing).
- **`Light.AspNetCore.Cors.CorsExtensions`** — `AllowOrigins(policyName, origins)` (specific origins, any
  method/header, credentials allowed) and `AllowAnyOrigins(policyName)` (any origin/method/header, no
  credentials) as `CorsOptions` extension helpers for `services.AddCors(...)`.

## Notes

- **Breaking rename:** `ApiControllerBase.Ok()` / `Ok<T>()` were renamed to `Success()` / `Success<T>()`.
  The old no-arg `Ok()` used the `new` keyword to shadow `ControllerBase.Ok()` with different (envelope-
  wrapping) behavior, which was confusing and easy to call by mistake expecting the base MVC behavior.
  Consumers on the old API must update call sites.
- **Breaking rename:** `ExceptionHandlerOptions.HideUndentifyException` (misspelled) was renamed to
  `HideUnidentifiedException`. Update any configuration binding (`appsettings.json` keys, `Configure<T>`
  lambdas) that referenced the old property name.
- **Bug fix:** `OrderedConverterBase<T>`'s constructor now strips **both** `BaseFirstOrderedConverterFactory`
  and `PropertyOrderedConverterFactory` from its internal safe-options clone (previously only one factory
  type was stripped), preventing a possible `StackOverflowException` when serializing self-referencing or
  nested types decorated with `[PropertyOrder]` alongside the base-first ordering converter.
- `PropertyOrderedConverterFactory` is defined but not added by `AddDefaultJsonOptions()` — only
  `BaseFirstOrderedConverterFactory` is wired in by default; add the property-ordered factory manually if
  your types use `[PropertyOrder]` instead of `[JsonPropertyOrder]`.
