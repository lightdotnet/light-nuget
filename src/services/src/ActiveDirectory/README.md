# Lightsoft.ActiveDirectory

Pluggable Active Directory / LDAP authentication and user-lookup services, exposed behind a single `IActiveDirectoryService` abstraction. Consumers register one of three backing implementations depending on their environment (no AD available, classic Windows AD, or cross-platform LDAP bind) without changing any calling code.

- **NuGet package id / assembly name:** `Lightsoft.ActiveDirectory` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.ActiveDirectory` — types live under `Light.ActiveDirectory`, `Light.ActiveDirectory.Interfaces`, `Light.ActiveDirectory.Dtos`, `Light.ActiveDirectory.Services`, and the registration helpers live under `Light.Extensions.DependencyInjection`
- **Target framework:** net10.0
- **Dependencies:** `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options`, `Novell.Directory.Ldap.NETStandard`, `System.DirectoryServices.AccountManagement`. No `ProjectReference`s — this is a leaf project. (Despite referencing `Microsoft.Extensions.Options`, no type in this package uses `IOptions<T>` — see [Notes](#notes).)

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `IActiveDirectoryService` | `Light.ActiveDirectory.Interfaces` | Common contract for authenticating against and reading users from a directory service. |
| `ActiveDirectoryService` | `Light.ActiveDirectory.Services` | Implementation backed by `System.DirectoryServices.AccountManagement`, for classic Windows domain-joined AD. `[SupportedOSPlatform("windows")]`. |
| `LDAPService` | `Light.ActiveDirectory.Services` | Implementation that binds over LDAP using `Novell.Directory.Ldap`. Also carries `[SupportedOSPlatform("windows")]` — see [Notes](#notes) for why. |
| `FakeActiveDirectoryService` | `Light.ActiveDirectory.Services` | No-op stand-in with no platform restriction — always reports unconfigured/unauthenticated. |
| `DomainOptions` | `Light.ActiveDirectory` | Configuration consumed by `ActiveDirectoryService` (the AD domain name). |
| `LdapOptions` | `Light.ActiveDirectory` | Configuration consumed by `LDAPService` (server address/port, bind DNs, admin credentials). |
| `DomainUserDto` | `Light.ActiveDirectory.Dtos` | Record returned by `GetByUserNameAsync` with the looked-up user's basic profile fields. |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddActiveDirectory()`, `AddActiveDirectory(Action<DomainOptions>)`, `AddLdapActiveDirectory(Action<LdapOptions>)` registration helpers. |

## `IActiveDirectoryService`

```csharp
public interface IActiveDirectoryService
{
    bool IsConfigured();
    Task<bool> CheckPasswordSignInAsync(string userName, string password);
    bool ChangePassword(string userName, string newPassword);
    Task<DomainUserDto?> GetByUserNameAsync(string userName);
}
```

- `IsConfigured()` — whether the directory backend has usable configuration.
- `CheckPasswordSignInAsync(userName, password)` — verifies credentials against the directory.
- `ChangePassword(userName, newPassword)` — administrative password reset. **Synchronous**, unlike the other members on this interface, even though the underlying implementations perform I/O.
- `GetByUserNameAsync(userName)` — looks up a user and maps it to a `DomainUserDto`. **Not implemented by `LDAPService`** — calling it there throws `NotImplementedException`.

## Implementations

### `ActiveDirectoryService`

Uses `System.DirectoryServices.AccountManagement.PrincipalContext`/`UserPrincipal` against a classic Windows AD domain, constructed with `DomainOptions`.

- `IsConfigured()` — `true` when `settings.Name` is non-empty.
- `CheckPasswordSignInAsync` — finds the user by identity, checks the account isn't locked out, then validates credentials via `PrincipalContext.ValidateCredentials`.
- `ChangePassword` — finds the user by identity and calls `UserPrincipal.SetPassword` + `Save()`; returns `false` if the user isn't found.
- `GetByUserNameAsync` — finds the user by identity and maps `UserPrincipalName`/`GivenName`/`Surname`/`VoiceTelephoneNumber`/`EmailAddress` into a `DomainUserDto`; returns `null` if not found.

### `LDAPService`

Uses `Novell.Directory.Ldap` for the credential-check path and `System.DirectoryServices.DirectoryEntry`/`DirectorySearcher` for password changes, constructed with `LdapOptions`.

- `IsConfigured()` — always `true`.
- `CheckPasswordSignInAsync` — returns `false` immediately for an empty/whitespace password; otherwise opens an `LdapConnection` (`SecureSocketLayer = false`) to `settings.Address`/`settings.Port` and binds as `"{userName}@{settings.Name}"` with the supplied password. A successful bind (no exception) is treated as a valid sign-in.
- `ChangePassword` — binds to `settings.Connection` as `settings.UserName`/`settings.Password` via `DirectoryEntry`, searches for `sAMAccountName = userName`, and invokes `SetPassword` + `CommitChanges` on the match. Returns `false` if no matching entry is found.
- `GetByUserNameAsync` — **throws `NotImplementedException`** unconditionally.

### `FakeActiveDirectoryService`

No-op implementation with no `[SupportedOSPlatform]` restriction, intended for local development/testing without a real directory available.

- `IsConfigured()` → `false`
- `CheckPasswordSignInAsync` → `Task.FromResult(false)`
- `ChangePassword` → `false`
- `GetByUserNameAsync` → `Task.FromResult<DomainUserDto?>(null)`

## Options

### `DomainOptions`
- `Name` (`string`, default `"domain.com"`) — the AD domain to connect to.
- `Enable` (`bool`, computed) — `!string.IsNullOrEmpty(Name)`. Note `ActiveDirectoryService.IsConfigured()` re-implements this same check inline rather than calling `Enable`.

### `LdapOptions`
- `Name` (`string`, default `"domain.com"`) — appended after `userName@` when binding.
- `Address` (`string`, default `"10.0.10.2"`) — LDAP server host.
- `Port` (`int`, default `389`).
- `Connection` (`string`, default `"LDAP://127.0.0.1/DC=company,DC=local"`) — bind path used by `ChangePassword`.
- `NewUserConnection` (`string`, default `"LDAP://127.0.0.1/ou=new_users,DC=company,DC=local"`) — declared but not read by any method in this package.
- `UserName` / `Password` (`string`, defaults `"admin"` / `"AdminP@ssword"`) — admin credentials used to bind for `ChangePassword`.

## `DomainUserDto`

```csharp
public record DomainUserDto(string UserName)
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}
```

`UserName` is a positional (constructor-required) record parameter; the remaining profile fields are ordinary mutable (`get; set;`, not `init`) properties, so instances can still be mutated after construction despite being a `record`.

## Registration methods

All three extension methods live in `Light.Extensions.DependencyInjection.ServiceCollectionExtensions`, and all three register `IActiveDirectoryService` with `AddTransient`. Pick the one that matches your environment — they are not interchangeable:

| Method | Registers | When to use |
|---|---|---|
| `AddActiveDirectory()` | `FakeActiveDirectoryService` | Testing / local development where no real directory is reachable. Every call reports unconfigured/unauthenticated/not-found — do not leave this wired up in production. |
| `AddActiveDirectory(Action<DomainOptions> configure)` | `ActiveDirectoryService` | Classic Windows-domain-joined AD via `System.DirectoryServices.AccountManagement`. Windows-only. |
| `AddLdapActiveDirectory(Action<LdapOptions> configure)` | `LDAPService` | Cross-platform-looking LDAP bind via `Novell.Directory.Ldap` — but see [Notes](#notes), the service as a whole is still Windows-only in this package. |

All three build the options instance by invoking the configuration `Action` directly against a `new DomainOptions()`/`new LdapOptions()` — options are **not** registered with `Microsoft.Extensions.Options` (no `IOptions<DomainOptions>`/`IOptions<LdapOptions>` available for injection elsewhere, no reload-on-change, no validation pipeline).

## Usage

### 1. Register a backend at startup

```csharp
// Local/dev — no directory available:
builder.Services.AddActiveDirectory();

// Classic Windows AD:
#pragma warning disable CA1416
builder.Services.AddActiveDirectory(opt => opt.Name = "company.local");
#pragma warning restore CA1416

// Cross-platform LDAP bind:
#pragma warning disable CA1416
builder.Services.AddLdapActiveDirectory(opt =>
{
    opt.Address = "10.0.10.2";
    opt.Port = 389;
    opt.Name = "company.local";
    opt.Connection = "LDAP://10.0.10.2/DC=company,DC=local";
    opt.UserName = "admin";
    opt.Password = "AdminP@ssword";
});
#pragma warning restore CA1416
```

Only pick one; the last registration of `IActiveDirectoryService` wins if you call more than one of these.

### 2. Inject `IActiveDirectoryService`

```csharp
[Route("[controller]")]
[ApiController]
public class ActiveDirectoryController(IActiveDirectoryService activeDirectoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string user)
        => Ok(await activeDirectoryService.GetByUserNameAsync(user));

    [HttpGet("check_password")]
    public async Task<IActionResult> CheckPassword(string user, string password)
        => Ok(await activeDirectoryService.CheckPasswordSignInAsync(user, password));
}
```

Because everything is behind `IActiveDirectoryService`, controllers/services never need to know which of the three backends is actually wired up.

## Notes

- `AddActiveDirectory(Action<DomainOptions>)` and `AddLdapActiveDirectory(Action<LdapOptions>)` are annotated `[SupportedOSPlatform("windows")]`, and calling them from code that isn't itself marked as Windows-only will produce a `CA1416` platform-compatibility warning — suppress it explicitly (as shown above) or guard the call with `OperatingSystem.IsWindows()`.
- `LDAPService` is marked `[SupportedOSPlatform("windows")]` at the class level even though its `CheckPasswordSignInAsync` path uses the cross-platform `Novell.Directory.Ldap` library. The reason is `ChangePassword`, which uses `System.DirectoryServices.DirectoryEntry`/`DirectorySearcher` — a Windows-only API in .NET — and the platform attribute applies to the whole class, not per-member. In practice this means `LDAPService` cannot be used on non-Windows hosts even for the LDAP-bind sign-in path, despite the underlying library supporting it.
- `LDAPService.GetByUserNameAsync` always throws `NotImplementedException`; don't call it against this implementation.
- `IActiveDirectoryService.ChangePassword` is synchronous on all three implementations, including `ActiveDirectoryService` and `LDAPService`, which perform blocking directory I/O on the calling thread.
- `AddActiveDirectory()` (the no-argument overload) silently wires up `FakeActiveDirectoryService`. There is no compile-time signal distinguishing it from the real overloads other than the argument list — double-check which overload is actually being called in a given environment's startup code.
- None of the three registration methods use `Microsoft.Extensions.Options`; `DomainOptions`/`LdapOptions` instances are private to the closure created inside each extension method and are not resolvable elsewhere via DI.
- `LdapOptions.NewUserConnection` is defined but not consumed anywhere in this package's current code.
