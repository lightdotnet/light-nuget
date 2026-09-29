[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.Serilog

Serilog wiring for ASP.NET Core `Generic Host` applications. The package configures a `Serilog.LoggerConfiguration` with a Console + Debug baseline, optional rolling-file and Elasticsearch sinks driven by `appsettings.json`, standard enrichers (machine name, environment, application name), and layers in whatever the `Serilog` configuration section itself specifies. It also ships a small standalone bootstrap logger for use before/outside the host-builder pipeline.

- **NuGet package id / assembly name:** `Lightsoft.Serilog` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.Serilog` — every type in this package lives directly under `Light.Serilog`
- **Target framework:** netstandard2.1
- **Dependencies:** `Serilog.AspNetCore`, `Elastic.Serilog.Sinks`, `Serilog.Sinks.Async`, `Serilog.Sinks.Console`, `Serilog.Enrichers.Environment` (see `Serilog.csproj`). `Serilog.Sinks.File` and `Serilog.Sinks.Debug` are pulled in transitively (via `Serilog.AspNetCore`) — there's no direct `PackageReference` to either, but the code uses both `.File(...)` and `.Debug()` sinks. No `ProjectReference`s — this is a leaf project.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `SerilogHostBuilderExtensions` | `Light.Serilog` | Static class with the single entry point `ConfigureSerilog(this IHostBuilder host)`, which calls `host.UseSerilog(SerilogConfigurationExtensions.Configure)`. |
| `SerilogConfigurationExtensions` | `Light.Serilog` | Static class holding the actual `Configure` delegate (`Action<HostBuilderContext, LoggerConfiguration>`) plus the private helper methods (`BaseConfig`, `WriteToFile`, `WriteToElasticsearch`) that build up the `LoggerConfiguration`. This is where all the sink/enricher logic lives. The Elasticsearch sink itself is the internal `ElasticsearchDailyIndexSink`. |
| `SerilogOptionsExtensions` | `Light.Serilog` | Static class with `GetWriteToOptions(IConfiguration)` and `GetWriteTo(IConfiguration, string firstElementName)` — reads the `Serilog:WriteTo` array out of configuration and finds the first entry whose `Name` matches exactly. |
| `WriteToOptions` | `Light.Serilog` | Plain binding class for one `Serilog:WriteTo` array entry: `string Name` and `Dictionary<string, string>? Args`. |
| `Serilogger` | `Light.Serilog` | A small standalone class (not `static` — it's `public class Serilogger` with only static members) providing `Initialize()` and `EnsureInitialized()`. See [Bootstrap logger](#bootstrap-logger-serilogger) below. |

There's also a `logger.json` file at the root of this project. It is **not** referenced by any `Compile`/`Content` item in `Serilog.csproj` and isn't read by any of the code above — it's a loose sample/reference config file showing a fuller `Serilog` JSON shape (filters, an MSSQL-flavored section, a commented-out `Seq` sink, etc.), not something this package loads at runtime.

## How `SerilogConfigurationExtensions.Configure` builds the logger

`Configure` is invoked by `Serilog.AspNetCore`'s `UseSerilog` with the host's `HostBuilderContext` and a fresh `LoggerConfiguration`. It resolves `applicationName` from `context.HostingEnvironment.ApplicationName` (lower-cased, `.` replaced with `-`; falls back to `"UnknownApp"`) and `environment` from `context.HostingEnvironment.EnvironmentName` (falls back to `"Development"`), then pipes the configuration through, in order:

1. **`BaseConfig()`** — always adds `WriteTo.Async(c => c.Debug())` and `WriteTo.Async(c => c.Console())`. (There are commented-out lines here for a message-template exclusion filter and several `MinimumLevel.Override` calls — currently inactive, kept as reference/dead code, not applied.)
2. **`WriteToFile(configuration, applicationName, environment)`** — looks up a `Serilog:WriteTo` entry whose `Name` is exactly `"FileAsync"`. If none exists, this is a no-op. If it exists (even with no `Args`), it adds a `WriteTo.Async(c => c.File(...))` sink writing to `Path.Combine(path, "{applicationName}-{environment}-log-.txt")` (platform-correct separator), daily rolling, 50 MB roll-on-size-limit, `shared: true`. `Path` and `Template` can be overridden via that entry's `Args`; otherwise they default to `"logs"` and `"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{SourceContext}{NewLine}{Exception}"`.
3. **`WriteToElasticsearch(configuration, applicationName, environment)`** — looks up a `Serilog:WriteTo` entry whose `Name` is exactly `"ElasticsearchAsync"`. Requires that entry's `Args` to include non-empty `Endpoint`, `Username`, and `Password` (`ServiceName` is optional and defaults to `applicationName`); otherwise it's a no-op. When active, it adds `WriteTo.Async(...)` around this package's internal `ElasticsearchDailyIndexSink` (built on Elastic.Ingest's `EcsIndexChannel`, ECS documents via `EcsTextFormatterConfiguration<LogEventEcsDocument>`, basic auth from `Username`/`Password`). Each event goes to a **plain index for the UTC date of that event**: `{serviceName}-{environment}-{yyyy-MM-dd}-generic-default` (e.g. `wtcvn-api-production-2026-09-29-generic-default`). The date is always formatted with the invariant (Gregorian) culture, whatever the process culture — Elastic.Ingest formats it with the current culture, so previously a th-TH, fa-IR or ar-SA host wrote to e.g. `...-2569-09-29-...` (Buddhist year). The index rolls over at 00:00 UTC with no restart. `serviceName` and `environment` are lower-cased with `ToLowerInvariant()`; only characters Elasticsearch forbids in index names (`\ / * ? " < > | , #`, space, `:`) and `{`/`}` become `_`, and `-` is kept. On startup the sink installs the ECS component templates and a plain-index template for `{serviceName}-{environment}-*-generic-default` using `BootstrapMethod.Failure` (see [Notes](#notes)). On shutdown it flushes buffered events (waiting up to 10 s) and then disposes the channel.
4. **Enrichers** — `Enrich.FromLogContext()`, `Enrich.WithMachineName()`, `Enrich.WithProperty("Environment", environment)`, `Enrich.WithProperty("Application", applicationName)`.
5. **`ReadFrom.Configuration(context.Configuration)`** — applied last, so anything declared directly under the `Serilog` section (e.g. `MinimumLevel`, `MinimumLevel:Override`, additional `Enrich`/`Filter`/`WriteTo` entries understood by `Serilog.Settings.Configuration`) is layered on top of everything configured programmatically above.

## Bootstrap logger (`Serilogger`)

`Serilogger` is a separate, minimal helper distinct from the `ConfigureSerilog`/`Configure` host-builder path above. It's meant for code paths that run before the host is built (e.g. wrapping `CreateHostBuilder().Build().Run()` in a `try/catch` to log startup failures) or outside DI entirely:

- `Initialize()` — builds a bare `LoggerConfiguration` with `Enrich.FromLogContext()` and `WriteTo.Console()` only (no async wrapping, no file/Elasticsearch sinks, no `Serilog:*` configuration binding) and returns the built `ILogger`.
- `EnsureInitialized()` — idempotently assigns `Log.Logger = Initialize()` exactly once, guarded by a private static flag checked under a lock, so it is thread-safe: concurrent callers assign `Log.Logger` only once, and every caller returns only after it has been assigned.

Because `Initialize()`/`EnsureInitialized()` don't read any configuration, the logger they produce is intentionally lower-fidelity than the one built by `ConfigureSerilog` — use it only as a fallback for the narrow bootstrap window before `ConfigureSerilog` has run, not as an alternative to it.

## Usage

### Program.cs

```csharp
using Light.Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureSerilog();

// ... other service registrations, builder.Build(), app.Run(), etc.
```

This is exactly how the sample `WebApi` project wires it up (`src/plugins/samples/WebApi/Program.cs`).

### appsettings.json

`ConfigureSerilog` reads from the `Serilog` section, including a `WriteTo` array matched by exact `Name`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Error",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "ElasticsearchAsync",
        "Args": {
          "ServiceName": "test-service",
          "Endpoint": "http://10.114.1.27:9200",
          "Username": "elastic",
          "Password": "elastic"
        }
      },
      {
        "Name": "FileAsync"
      }
    ]
  }
}
```

`MinimumLevel`/`MinimumLevel:Override` are picked up by the `ReadFrom.Configuration(context.Configuration)` step; `WriteTo` entries are read directly by this package's own `WriteToFile`/`WriteToElasticsearch` methods (via `SerilogOptionsExtensions.GetWriteTo`), not by `ReadFrom.Configuration`'s own `WriteTo` handling.

## Notes

- `SerilogOptionsExtensions.GetWriteTo` matches `WriteToOptions.Name` by exact string equality. The sample `WebApi/appsettings.json` currently has an entry named `"ElasticsearchAsync1"` (not `"ElasticsearchAsync"`) — because of the trailing `1`, `WriteToElasticsearch` will not match it and the Elasticsearch sink silently will not be configured for that sample, regardless of its `Args`. Double-check the exact `Name` values (`"FileAsync"`, `"ElasticsearchAsync"`) when wiring up configuration.
- `WriteToFile` triggers off the mere presence of a `"FileAsync"` entry — it does not require `Args` to be set. An empty `{ "Name": "FileAsync" }` entry is enough to enable file logging with default path/template.
- **Behavior change — daily indices per event date instead of a startup-dated data stream.** Earlier versions wrote to a *data stream* named `{serviceName}-{environment}-{yyyy-MM-dd}-generic-default`, with the date taken once at app start and the name not lower-cased. A long-running process therefore wrote to a single "day" until it restarted, and a name with upper-case parts was rejected. The name layout is unchanged, but the parts are now lower-case, the date is each event's UTC date, and the targets are **plain indices, not data streams**. A Kibana data view such as `{serviceName}-{environment}-*` matches both old and new data. If the app already ran today before upgrading, today's name already exists as a data stream, and today's events keep going into it (bulk `create` into a data stream is valid). Only new days produce plain indices.
- Index template: the sink installs `{serviceName}-{environment}-template-generic-default-{ecsVersion}` at the stock ECS priority **+1**, without `data_stream`. The previous sink left one data-stream template per start date (`{service}-{env}-{date}-generic-*`) at the stock priority, and Elasticsearch rejects a new template that overlaps one of those at equal priority. The old templates can be deleted once no longer needed. No ILM or retention policy is set; delete old daily indices yourself or attach an ILM policy to the template.
- `BootstrapMethod.Failure` is still used: if Elasticsearch is unreachable or rejects the templates at startup, the sink constructor throws and the application fails to start, as before. The safe alternative is `BootstrapMethod.Silent`, which attempts the bootstrap and ignores failures. It was not switched, to keep the old semantics.
- `Serilogger` is a `public class`, not `public static class`, even though both its members are `static`; it can technically be instantiated, though there's no reason to (making it `static` would be a breaking change).
- The package targets `netstandard2.1` but references `Serilog.AspNetCore` 10.x, which on that TFM pulls in the legacy ASP.NET Core 2.x abstractions packages; consumers on modern .NET get the framework versions instead.
- `logger.json` in this project is a reference/sample file only — it is not loaded by any code here and is not packaged into consuming projects.
