# Lightsoft.AspNetCore.Swagger

NuGet package id: **`Lightsoft.AspNetCore.Swagger`** (root namespace `Light.AspNetCore.Swagger`).

A thin, configuration-driven wrapper around Swashbuckle (`Swashbuckle.AspNetCore.SwaggerGen` / `SwaggerUI`) and `Asp.Versioning.Mvc.ApiExplorer` that wires up Swagger/OpenAPI generation and the Swagger UI from an `appsettings.json` section, with optional JWT/Basic security scheme support and API-version-aware document generation.

This is a leaf project — it has no `ProjectReference` to other projects in the `Framework` solution.

## Dependencies

- `Asp.Versioning.Mvc.ApiExplorer` (`10.*`)
- `Swashbuckle.AspNetCore.SwaggerGen` (`10.*`)
- `Swashbuckle.AspNetCore.SwaggerUI` (`10.*`)

## Installation

```
dotnet add package Lightsoft.AspNetCore.Swagger
```

## Usage

In `Program.cs` (or service registration):

```csharp
using Light.AspNetCore.Swagger;

builder.Services.AddSwagger(builder.Configuration);
```

In the middleware pipeline:

```csharp
app.UseSwagger();
```

Both extension methods are no-ops when `Swagger:Enable` is `false` (or the `Swagger` config section is missing values), so they are safe to call unconditionally, including in production.

## Configuration (`SwaggerOptions`)

Bound from the `"Swagger"` configuration section:

```json
{
  "Swagger": {
    "Enable": true,
    "Title": "My Service API",
    "SecurityScheme": "jwt",
    "VersionDefinition": true
  }
}
```

| Property | Type | Description |
|---|---|---|
| `Enable` | `bool` | Master switch. When `false`, `AddSwagger` registers nothing and `UseSwagger` skips the middleware entirely. |
| `Title` | `string?` | Sets the OpenAPI document's `Info.Title` (via `TitleFilter`, an `IDocumentFilter`) and the Swagger UI browser tab title (via `CustomSwaggerUIOptions`). Ignored if null/empty. |
| `SecurityScheme` | `string?` | Adds security scheme(s) to the generated document. The value is matched with `Contains`, not equality, so `"jwt"`, `"basic"`, or a combined value such as `"jwt,basic"` will add the corresponding scheme(s). Any other substring is ignored. |
| `VersionDefinition` | `bool` | When `true`, generates one Swagger document per discovered API version (via `IApiVersionDescriptionProvider` from `Asp.Versioning.Mvc.ApiExplorer`) instead of a single default document, and exposes one Swagger UI endpoint per version. Requires API versioning + API explorer to already be registered by the consuming app (`AddApiVersioning().AddApiExplorer()`), otherwise resolving `IApiVersionDescriptionProvider` will throw. |

`SecurityScheme` values recognized:

- `"jwt"` — adds an HTTP `bearer` security scheme (`BearerFormat = "JWT"`) via `AddJwtSecurityScheme`.
- `"basic"` — adds an HTTP `basic` security scheme via `AddBasicSecurityScheme`.

## What `AddSwagger` registers

When `Enable = true`:

- `SwaggerOptions` bound and available as `IOptions<SwaggerOptions>`.
- If `VersionDefinition = true`: `VersionDefinitionSwaggerOptions` (`IConfigureOptions<SwaggerGenOptions>`) — adds a Swagger document per discovered API version and applies the `SwaggerDefaultValues` operation filter (marks deprecated operations, backfills missing parameter descriptions from model metadata).
- `AddSwaggerGen(...)` with:
  - Security scheme(s) per `SecurityScheme` (see above).
  - `CustomSchemaIds(x => x.FullName)` — avoids schema id collisions when multiple types share the same short name.
  - `DocumentFilter<TitleFilter>()` — applies `SwaggerOptions.Title` to the document.
  - `UseInlineDefinitionsForEnums()`.
- `CustomSwaggerUIOptions` (`IConfigureOptions<SwaggerUIOptions>`) — sets the Swagger UI document/tab title from `SwaggerOptions.Title`.

## What `UseSwagger` does

When `Enable = true`:

- Calls Swashbuckle's `UseSwagger(app)` to expose the `swagger.json` document(s).
- If `VersionDefinition = true`, calls `UseSwaggerUI` with one `SwaggerEndpoint` per API version group returned by `IApiVersionDescriptionProvider`.
- Otherwise calls `UseSwaggerUI()` with defaults (single document).

## Files

- `SwaggerServiceCollectionExtensions.cs` — `AddSwagger(IServiceCollection, IConfiguration)`.
- `SwaggerApplicationBuilderExtensions.cs` — `UseSwagger(IApplicationBuilder)`.
- `SwaggerOptions.cs` — bound options.
- `TitleFilter.cs` — `IDocumentFilter` that sets the document title from `SwaggerOptions.Title`.
- `CustomSwaggerUIOptions.cs` — `IConfigureOptions<SwaggerUIOptions>` that sets the UI tab title.
- `VersionDefinitionSwaggerOptions.cs` — `IConfigureOptions<SwaggerGenOptions>` that generates one document per API version.
- `SwaggerDefaultValues.cs` — `IOperationFilter` used by the version-per-document setup (deprecation flag, parameter description fallback).
- `SwaggerSecuritySchemeExtensions.cs` — internal helpers adding JWT/Basic HTTP security schemes and requirements.
