# Lightsoft.AspNetCore.Modularity

NuGet package ID: **`Lightsoft.AspNetCore.Modularity`** (see `Modularity.csproj`).

This project has no `ProjectReference` to any other project in the `Framework` solution — it's a leaf
package. Its only dependency is `FrameworkReference Microsoft.AspNetCore.App` (the shared ASP.NET Core
framework).

It provides two independent features that happen to live in the same package:

1. A lightweight **module system** for composing an ASP.NET Core app out of self-contained modules
   discovered via assembly scanning.
2. A simpler **convention-based DI auto-registration** feature, unrelated to modules.

## 1. Module system

Namespaces: `Light.AspNetCore.Modularity`, `Light.AspNetCore.Builder`, `Light.Extensions.DependencyInjection`.

- `AppModule` — abstract base class implementing all three module interfaces below with no-op virtual
  members, so a module only needs to override what it uses:
  - `IModuleServiceCollection.Add(IServiceCollection)` / `Add(IServiceCollection, IConfiguration)`
  - `IModuleBuilder.Use(IApplicationBuilder)`
  - `IModuleEndpoint.Map(IEndpointRouteBuilder)`
- `ModuleServiceCollectionExtensions.AddModules(configuration, assemblies)` — scans `assemblies` for
  types assignable to `AppModule` (or a custom `T : IModuleServiceCollection` via the generic overload),
  instantiates each with `Activator.CreateInstance`, and calls both `Add` overloads.
- `ModuleBuilderExtensions.UseModules(assemblies)` — same scan/instantiate pattern, calls `Use` on each
  module to configure the middleware pipeline. Generic overload accepts any `T : IModuleBuilder`.
- `ModuleBuilderExtensions.MapModuleEndpoints(assemblies)` — same pattern, calls `Map` on each module to
  register endpoints. Generic overload accepts any `T : IModuleEndpoint`.

All three scanning APIs delegate to the internal `AssemblyTypeExtensions.GetAssignableFrom<T>` helper.

### Usage

```csharp
// SomeModule.cs
public class OrdersModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOrderService, OrderService>();
    }

    public override void Use(IApplicationBuilder app)
    {
        app.UseMiddleware<OrderAuditMiddleware>();
    }

    public override void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/orders", () => Results.Ok());
    }
}
```

```csharp
// Program.cs
var assemblies = new[] { typeof(OrdersModule).Assembly };

builder.Services.AddModules(builder.Configuration, assemblies);

var app = builder.Build();

app.UseModules(assemblies);
app.MapModuleEndpoints(assemblies);
```

## 2. Convention-based DI auto-registration

Namespace: `Light.Extensions.DependencyInjection`.

- `ITransientDependency`, `IScopedDependency`, `ISingletonDependency` — empty marker interfaces.
- `AutoAddServiceExtensions.AutoAddDependencies()` — scans **every currently loaded assembly in
  `AppDomain.CurrentDomain`** (not a caller-supplied `Assembly[]`, unlike the module APIs above) for
  concrete, non-abstract classes implementing any of the three markers, and registers each with the
  matching `ServiceLifetime`.

### Usage

```csharp
public class OrderService : IOrderService, IScopedDependency
{
    // ...
}
```

```csharp
// Program.cs
builder.Services.AutoAddDependencies();
```

## Notes

- **Marker scanning scope differs between the two features.** The module APIs
  (`UseModules`/`MapModuleEndpoints`/`AddModules`) only scan the `Assembly[]` you pass in. `AutoAddDependencies()`
  ignores any assembly list and always scans all assemblies currently loaded in the `AppDomain`. Easy to
  mix up — pick the right one depending on whether you want scoped or global scanning.
- `AutoAddDependencies()` matches an implementation to an interface by an **exact `I<ClassName>` naming
  convention** — e.g. class `OrderService` only auto-binds to an interface literally named `IOrderService`.
  This was recently changed from a substring-contains match, which could non-deterministically pick the
  wrong interface when a class implemented multiple interfaces whose names all contained the class name.
  If no interface named `I<ClassName>` exists on the class, it is registered by concrete type only (no
  interface mapping).
- The internal `AssemblyTypeExtensions.GetAssignableFrom<T>` helper (used by the module-scanning APIs)
  falls back to scanning all currently-loaded `AppDomain` assemblies when the caller passes an empty or
  `null` assembly array.
