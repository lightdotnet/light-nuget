# Lightsoft.AspNetCore.Authorization

Permission-based authorization building blocks for ASP.NET Core. The package lets you use ordinary `[Authorize(Policy = "...")]` attributes as permission checks without having to pre-register every permission as a named policy with `AddAuthorization`.

- **NuGet package id / assembly name:** `Lightsoft.AspNetCore.Authorization` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light` — types live under `Light.AspNetCore.Authorization` and `Light.Extensions.DependencyInjection`
- **Target framework:** net10.0
- **Dependencies:** `Microsoft.AspNetCore.Authorization` only. No `ProjectReference`s — this is a leaf project in the `Framework` solution.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `PermissionPolicyProvider` | `Light.AspNetCore.Authorization` | `IAuthorizationPolicyProvider` that turns a policy name into a permission-based `AuthorizationPolicy` on the fly. |
| `PermissionRequirement(string Permission)` | `Light.AspNetCore.Authorization` | `IAuthorizationRequirement` record carrying the permission (= policy name) to check. |
| `PermissionAuthorizationHandler` | `Light.AspNetCore.Authorization` | Abstract `AuthorizationHandler<PermissionRequirement>`. You implement `HandleRequirementAsync` in a derived class to decide whether the current user has the permission. |
| `MustHavePermissionAttribute(string policy)` | `Light.AspNetCore.Authorization` | Thin `AuthorizeAttribute` subclass — sets `Policy` from the constructor argument, purely for readability at call sites. |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddPermissionPolicyProvider[<T>]` and `AddPermissionAuthorizationHandler<T>` registration helpers. |

## How `PermissionPolicyProvider` works

`GetPolicyAsync(policyName)` is called by the framework for every `[Authorize(Policy = "...")]` policy name it hasn't seen registered explicitly:

1. If a built policy for that name is already cached, it is returned immediately (see [Notes](#notes)).
2. Otherwise it calls the virtual `CheckPermissionValidAsync(policyName)`. **The base implementation always returns `true`** — by default, every policy name is treated as a valid permission and gets wrapped into a policy with a single `PermissionRequirement(policyName)`.
3. Only if `CheckPermissionValidAsync` returns `false` does it fall back to `FallbackPolicyProvider.GetPolicyAsync(policyName)` (an internal `DefaultAuthorizationPolicyProvider`), which resolves normally-registered named policies (`AddAuthorization(o => o.AddPolicy(...))`).

Because the default `CheckPermissionValidAsync` always returns `true`, **out of the box every policy name is resolved as a permission and the fallback path is effectively unreachable**. If you need some policy names to remain "ordinary" named policies (not permissions), derive from `PermissionPolicyProvider` and override `CheckPermissionValidAsync` to return `false` for those names — that's what makes the fallback to `DefaultAuthorizationPolicyProvider` actually trigger.

`GetDefaultPolicyAsync()` and `GetFallbackPolicyAsync()` delegate to the standard default provider / return `null`, matching normal ASP.NET Core policy-provider semantics for endpoints without an explicit policy.

You must also register at least one `IAuthorizationHandler` for `PermissionRequirement` — this package only provides the abstract `PermissionAuthorizationHandler` base class; the actual permission-evaluation logic (e.g. checking claims, a DB, etc.) is up to the consumer.

## Usage

### 1. Implement a handler

```csharp
public class MyPermissionHandler : PermissionAuthorizationHandler
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

### 2. Register services

```csharp
services.AddAuthorization();
services.AddPermissionPolicyProvider();               // or AddPermissionPolicyProvider<MyPolicyProvider>()
services.AddPermissionAuthorizationHandler<MyPermissionHandler>();
```

`AddPermissionPolicyProvider` registers `PermissionPolicyProvider` as `IAuthorizationPolicyProvider` (`AddSingleton`, replacing/overriding the framework default). `AddPermissionAuthorizationHandler<T>` registers `T` as `IAuthorizationHandler` (`AddScoped`).

### 3. Declare permissions on endpoints

Any policy name is treated as a permission, so no policy pre-registration is required:

```csharp
[Authorize(Policy = "Orders.Read")]
public IActionResult GetOrders() => ...;

// equivalent, using the readability wrapper
[MustHavePermission("Orders.Read")]
public IActionResult GetOrders() => ...;
```

`Orders.Read` is passed straight through as `PermissionRequirement.Permission`, and your `PermissionAuthorizationHandler` decides whether the current user has it.

## Notes

- `PermissionPolicyProvider.GetPolicyAsync` now caches built `AuthorizationPolicy` instances per policy name in a `ConcurrentDictionary<string, AuthorizationPolicy>`, instead of constructing a new `AuthorizationPolicyBuilder`/policy on every request for the same policy name.
- The fallback call `FallbackPolicyProvider.GetPolicyAsync(policyName)` is now active. Previously this call was commented out, so once `PermissionPolicyProvider` was registered, any normally-registered named policy (`AddAuthorization(o => o.AddPolicy(...))`) that wasn't treated as a permission would resolve to `null` and silently fail authorization. This is a correctness fix, not just a performance change — but note it only has an effect for consumers that override `CheckPermissionValidAsync` to return `false` for non-permission policy names, since the default implementation always returns `true`.
