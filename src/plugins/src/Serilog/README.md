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
| `SerilogHostBuilderExtensions` | `Light.Serilog` | Static class with the single entry point `ConfigureSerilog(this IHostBuilder host)`, which calls `host.UseSerilog(SerilogConfigurationExtensions.Configure)`. Renamed this session from a class literally called `Startup` — a misleading name for a library helper (it collided conceptually with an app's own `Startup` class and didn't describe what the method does). |
| `SerilogConfigurationExtensions` | `Light.Serilog` | Static class holding the actual `Configure` delegate (`Action<HostBuilderContext, LoggerConfiguration>`) plus the private helper methods (`BaseConfig`, `WriteToFile`, `WriteToElasticsearch`) that build up the `LoggerConfiguration`. This is where all the sink/enricher logic lives. |
| `SerilogOptionsExtensions` | `Light.Serilog` | Static class with `GetWriteToOptions(IConfiguration)` and `GetWriteTo(IConfiguration, string firstElementName)` — reads the `Serilog:WriteTo` array out of configuration and finds the first entry whose `Name` matches exactly. |
| `WriteToOptions` | `Light.Serilog` | Plain binding class for one `Serilog:WriteTo` array entry: `string Name` and `Dictionary<string, string>? Args`. |
| `Serilogger` | `Light.Serilog` | A small standalone class (not `static` — it's `public class Serilogger` with only static members) providing `Initialize()` and `EnsureInitialized()`. See [Bootstrap logger](#bootstrap-logger-serilogger) below. |

There's also a `logger.json` file at the root of this project. It is **not** referenced by any `Compile`/`Content` item in `Serilog.csproj` and isn't read by any of the code above — it's a loose sample/reference config file showing a fuller `Serilog` JSON shape (filters, an MSSQL-flavored section, a commented-out `Seq` sink, etc.), not something this package loads at runtime.

## How `SerilogConfigurationExtensions.Configure` builds the logger

`Configure` is invoked by `Serilog.AspNetCore`'s `UseSerilog` with the host's `HostBuilderContext` and a fresh `LoggerConfiguration`. It resolves `applicationName` from `context.HostingEnvironment.ApplicationName` (lower-cased, `.` replaced with `-`; falls back to `"UnknownApp"`) and `environment` from `context.HostingEnvironment.EnvironmentName` (falls back to `"Development"`), then pipes the configuration through, in order:

1. **`BaseConfig()`** — always adds `WriteTo.Async(c => c.Debug())` and `WriteTo.Async(c => c.Console())`. (There are commented-out lines here for a message-template exclusion filter and several `MinimumLevel.Override` calls — currently inactive, kept as reference/dead code, not applied.)
2. **`WriteToFile(configuration, applicationName, environment)`** — looks up a `Serilog:WriteTo` entry whose `Name` is exactly `"FileAsync"`. If none exists, this is a no-op. If it exists (even with no `Args`), it adds a `WriteTo.Async(c => c.File(...))` sink writing to `{path}\{applicationName}-{environment}-log-.txt`, daily rolling, 50 MB roll-on-size-limit, `shared: true`. `Path` and `Template` can be overridden via that entry's `Args`; otherwise they default to `"logs"` and `"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{SourceContext}{NewLine}{Exception}"`.
3. **`WriteToElasticsearch(configuration, applicationName, environment)`** — looks up a `Serilog:WriteTo` entry whose `Name` is exactly `"ElasticsearchAsync"`. Requires that entry's `Args` to include non-empty `Endpoint`, `Username`, and `Password` (`ServiceName` is optional and defaults to `applicationName`); otherwise it's a no-op. When active, it adds `WriteTo.Async(w => w.Elasticsearch(...))` targeting a data stream named `"{serviceName}-{environment}-{yyyy-MM-dd}"` (computed once, at startup — it does not roll to a new stream at midnight), using ECS formatting (`EcsTextFormatterConfiguration<LogEventEcsDocument>`), `BootstrapMethod.Failure`, and basic auth built from `Username`/`Password`.
4. **Enrichers** — `Enrich.FromLogContext()`, `Enrich.WithMachineName()`, `Enrich.WithProperty("Environment", environment)`, `Enrich.WithProperty("Application", applicationName)`.
5. **`ReadFrom.Configuration(context.Configuration)`** — applied last, so anything declared directly under the `Serilog` section (e.g. `MinimumLevel`, `MinimumLevel:Override`, additional `Enrich`/`Filter`/`WriteTo` entries understood by `Serilog.Settings.Configuration`) is layered on top of everything configured programmatically above.

A `WriteToMSSQL` helper exists in the source but is entirely commented out — it is not compiled and has no effect.

## Bootstrap logger (`Serilogger`)

`Serilogger` is a separate, minimal helper distinct from the `ConfigureSerilog`/`Configure` host-builder path above. It's meant for code paths that run before the host is built (e.g. wrapping `CreateHostBuilder().Build().Run()` in a `try/catch` to log startup failures) or outside DI entirely:

- `Initialize()` — builds a bare `LoggerConfiguration` with `Enrich.FromLogContext()` and `WriteTo.Console()` only (no async wrapping, no file/Elasticsearch sinks, no `Serilog:*` configuration binding) and returns the built `ILogger`.
- `EnsureInitialized()` — idempotently assigns `Log.Logger = Initialize()` exactly once, guarded by a private static `bool _initialized` flag. Fixed this session from a broken check that always evaluated as true (the previous guard used a type-based comparison rather than the boolean flag, so `Log.Logger` was silently reassigned on every call instead of only the first).

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
- The Elasticsearch data stream name embeds `DateTime.UtcNow:yyyy-MM-dd` at the time `Configure` runs (i.e. at host startup), not per log event — it does not roll to a new stream at midnight without an application restart.
- `Serilogger` is a `public class`, not `public static class`, even though both its members are `static`; it can technically be instantiated, though there's no reason to.
- `logger.json` in this project is a reference/sample file only — it is not loaded by any code here and is not packaged into consuming projects.
