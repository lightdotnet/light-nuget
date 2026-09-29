[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.AspNetCore.Extensions

NuGet package ID: **`Lightsoft.AspNetCore.Extensions`** (see `WebHost.csproj`; the `.csproj` file itself
is still named `WebHost.csproj` and the project folder is `WebHost`, but the assembly/package name is
`Lightsoft.AspNetCore.Extensions`). Version `2.0.1` at the time of writing (tracks `$(NugetVersion)` in `src/framework/Directory.Build.props`).

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
  returns `host` unchanged (default config). Otherwise same as the array overload with a single entry.
- `LoadConfigurationFrom(this IHostApplicationBuilder host, string[]? paths)` — null/empty/whitespace
  entries are ignored (no-op if none remain). For each path, in the given order, loads every `*.json` file
  whose name doesn't start with `appsettings` (case-insensitive), sorted by file name (ordinal), each with
  `optional: false, reloadOnChange: true`. Missing folders are skipped. Then — **once** — loads
  `appsettings.json` (required) and `appsettings.{EnvironmentName}.json` (optional) from the content root,
  and adds environment variables. Folder files are added *before* `appsettings.json`, so root
  `appsettings*.json` values (and env vars) win on conflicts; among folders, later paths win.
- Relative paths are resolved against `IHostEnvironment.ContentRootPath` (not the process working
  directory), and files are registered by their full path.

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
| `IncludeRequest` | `bool` | Logs the (minified, if valid JSON) request body. |
| `IncludeResponse` | `bool` | Logs the response body, captured by a write-through stream (the response is streamed to the client as usual, not buffered). |
| `ExcludePaths` | `List<string>?` | Additional path substrings to skip logging for. |
| `MaxBodyLogBytes` | `int` | Max bytes of each body captured for logging (default `32768`); longer bodies are logged with a `...[truncated]` suffix. `0` disables body logging. |

`RequestLoggingMiddleware` always merges `"hangfire"` and `"swagger"` into the exclude list in addition to
whatever `ExcludePaths` you configure — a request is skipped if its path *contains* any excluded substring.
Every non-excluded request is timed with a `Stopwatch` and logged with method, status code, path, query,
scheme, client IP, and `HttpContext.TraceIdentifier`. The summary line is written in a `finally`, so requests
that fail with an unhandled exception are logged too (at `Warning`, with the exception type in place of the
not-yet-final status code) before the exception is rethrown.

Body logging details:

- Only textual content types are logged (`text/*`, `*json*`, `*xml*`, `x-www-form-urlencoded`,
  `javascript`, `graphql`); bodies with no or binary content type (e.g. `multipart/form-data`,
  `application/octet-stream`) and compressed responses (`Content-Encoding` set) are not logged.
- The request body is read only up to `MaxBodyLogBytes` (with `HttpContext.RequestAborted`) and rewound for
  downstream middleware. Malformed JSON is logged raw instead of throwing (previously it turned a 400 into a 500).

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
- Client disconnects (`OperationCanceledException` while `HttpContext.RequestAborted` is cancelled) are
  logged at `Information`, the status is set to `499` if the response hasn't started, and no body is written.
- If the response has already started, nothing is written and the status code is not touched: the exception
  is logged (with stack trace) and reported as unhandled — `ExceptionHandler.TryHandleAsync` returns `false`
  and `ExceptionHandlerMiddleware` rethrows, so the server aborts the connection.
- Walks the `InnerException` chain and stops at the first `Light.Exceptions.ExceptionBase`; if there is none,
  uses the innermost exception.
- Maps `ValidationException` → its `StatusCode`, joining `ValidationErrors` into a `key: v1,v2|...` message;
  any other `ExceptionBase` → its own `StatusCode`; `KeyNotFoundException` → 404; anything else → 500, with
  the message replaced by `"Internal Server Error"` when `ExceptionHandlerOptions.HideUnidentifiedException`
  is `true` (default `true`).
- Logs the exception object (so the stack trace is kept) plus source type + message, then writes a
  `Light.Contracts.Result` JSON body (camelCase, nulls omitted) containing `Code`, `Message`, and
  `RequestId` (= `TraceIdentifier`).

`ExceptionHandlerOptions` is bound the normal `IOptions<T>` way; register/configure it via your own
`services.Configure<ExceptionHandlerOptions>(...)` before calling either registration mechanism.

## JSON property ordering

Namespace: `Light.Extensions.Json`. `MvcBuilderExtensions.AddDefaultJsonOptions()`
(`Light.Extensions.DependencyInjection`) sets `ReferenceHandler.IgnoreCycles`, camelCase naming,
case-insensitive property matching, a `JsonStringEnumConverter`, and **base-class-first property ordering**
via a contract-resolver modifier: it wraps the existing `TypeInfoResolver` (or a new
`DefaultJsonTypeInfoResolver`) with `.WithAddedModifier(JsonPropertyOrderModifiers.BaseFirst)`.

`JsonPropertyOrderModifiers` (static, `Action<JsonTypeInfo>` modifiers):

- **`BaseFirst`** — orders properties by the inheritance depth of the type that declares them (base class
  first; overridden virtual properties keep their base position), then by `[JsonPropertyOrder]`, then by
  declaration order.
- **`PropertyOrder`** — orders by `[PropertyOrder]` (`PropertyOrderAttribute`, from `SharedKernel`, same
  `Light.Extensions.Json` namespace; missing = 0), then `[JsonPropertyOrder]`, then declaration order. Not
  wired in by default.

Because they only set `JsonPropertyInfo.Order`, all other System.Text.Json behavior is preserved:
`[JsonIgnore]` (incl. conditions), `[JsonPropertyName]`, naming policy, `DefaultIgnoreCondition`, custom
converters, source-generated contexts, and no per-DTO reflection on each write.

```csharp
services.AddControllers().AddDefaultJsonOptions();

// or manually
options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
    .WithAddedModifier(JsonPropertyOrderModifiers.PropertyOrder);
```

**Legacy converters (`[Obsolete]`)** — `BaseFirstOrderedConverterFactory` / `BaseFirstOrderedConverter<T>`,
`PropertyOrderedConverterFactory` / `PropertyOrderedConverter<T>` and their base `OrderedConverterBase<T>`
remain public for existing consumers but are no longer registered by `AddDefaultJsonOptions()`. Their
`Write()` now skips `[JsonIgnore]` (and honors `WhenWritingNull`/`WhenWritingDefault`, plus the options'
`DefaultIgnoreCondition`), indexers and properties without a public getter, and honors `[JsonPropertyName]`.
Prefer the modifiers above.

Also in `Light.AspNetCore.Mvc`: `OrderedPropertiesJsonTypeInfoResolver`, a
`DefaultJsonTypeInfoResolver` override that assigns `JsonPropertyInfo.Order` alphabetically by property
name for every object type — a different ordering strategy, not wired in by default.

## MVC base controllers and conventions

Namespace: `Light.AspNetCore.Mvc`.

- **`ApiControllerBase`** — abstract `[ApiController]` with route `api/[controller]`. Exposes
  `Ok()` (declared `new`, shadowing `ControllerBase.Ok()`) and `Ok<T>(T data)` (an overload next to
  `ControllerBase.Ok(object?)`; overload resolution picks it for typed arguments), both virtual and `[ApiExplorerSettings(IgnoreApi = true)]`,
  wrapping the result
  in the framework's `Result`/`Result<T>` envelope (`Light.Contracts`), stamping `RequestId` with
  `HttpContext.TraceIdentifier`, and converting to an `IActionResult` via `ActionResultExtensions.ToActionResult()`
  (status code taken from `result.ToHttpStatusCode()`). `Ok<T>` passes a `ResultBase` argument through
  as-is instead of re-wrapping it.
- **`VersionedApiController`** — abstract, derives from `ApiControllerBase`, overrides the route to
  `api/v{version:apiVersion}/[controller]` and applies `[ApiVersion("1.0")]` (uses `Asp.Versioning`).
- **`LowercaseControllerNameConvention`** — internal `IControllerModelConvention`; lowercases a
  controller's name and inserts a separator (default `"_"`) before each uppercase letter after the first
  (e.g. `OrderItems` → `order_items`). Registered via `MvcBuilderExtensions.AddLowercaseControllers()`
  (the no-argument overload). The `AddLowercaseControllers(IControllerModelConvention)` overload registers
  **only** the convention you pass — it replaces the built-in lowercase convention rather than adding to it,
  despite the method name.
- **`AddInvalidModelStateHandler()`** (`MvcBuilderExtensions`) — replaces the default 400 response for
  invalid model state with a `Light.Contracts.Result` (`Code = ResultCode.BadRequest`), joining field
  errors into a `Model_prop: err1,err2|...` message.

```csharp
public class OrdersController : ApiControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(Guid id) => Ok(order);
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
  method/header, credentials allowed; throws `ArgumentException` if `origins` is empty or contains an
  empty value or a `*` wildcard, since wildcards must not be combined with credentials) and `AllowAnyOrigins(policyName)` (any origin/method/header, no
  credentials) as `CorsOptions` extension helpers for `services.AddCors(...)`.

## Notes

- `ApiControllerBase` exposes `Ok()` / `Ok<T>()` (not `Success()`): the no-arg `Ok()` uses `new` to shadow
  `ControllerBase.Ok()` with envelope-wrapping behavior — keep this in mind when you expect the base MVC
  behavior. Call `base.Ok(...)` explicitly for a plain `OkObjectResult`.
- **Breaking rename:** `ExceptionHandlerOptions.HideUndentifyException` (misspelled) was renamed to
  `HideUnidentifiedException`. Update any configuration binding (`appsettings.json` keys, `Configure<T>`
  lambdas) that referenced the old property name.
- **Behavior change (JSON):** `AddDefaultJsonOptions()` no longer adds `BaseFirstOrderedConverterFactory`;
  base-first ordering is done by the `JsonPropertyOrderModifiers.BaseFirst` resolver modifier. Output now
  honors `[JsonIgnore]`, `[JsonPropertyName]` and `DefaultIgnoreCondition` (previously ignored), and types
  with indexers/write-only properties no longer throw. If you set a `TypeInfoResolver` (e.g. a source-generated
  `JsonSerializerContext`) *before* calling `AddDefaultJsonOptions()`, it is wrapped; if you set it *after*,
  you replace the ordering modifier.
- **Behavior change (exception handling):** when the response has already started, the handler no longer
  sets `StatusCode` (which threw) and reports the exception as unhandled (`ExceptionHandlerMiddleware`
  rethrows). Client aborts return `499` without an error log. Error logs now include the exception object.
- **Behavior change (request logging):** response bodies are no longer fully buffered; body logging is capped
  by `MaxBodyLogBytes` and limited to textual content types; failed requests are logged.
- **Behavior change (configuration):** `LoadConfigurationFrom(string[])` adds root `appsettings*.json` and
  environment variables once (not once per path) and resolves relative paths against the content root.
- **Behavior change (CORS):** `AllowOrigins` throws `ArgumentException` for empty origins or `*` wildcards.
