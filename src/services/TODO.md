# TODO — Integration Services

Open follow-ups for the `IntegrationServices` solution (`IntegrationServices.slnx`), covering
`ActiveDirectory`, `Caching`, `FileGenerator`, `Graph`, `Mail.Contracts`, `Serilog`, `SmtpMail`
(`src/`), `tests/UnitTests`, and the `samples/WebApi` sample. Generated from a dependency,
architecture, and code-quality pass on 2026-07-29 — see `README.md` for user-facing docs.

## Correctness (shipped bugs — fix first)

- [x] **`Caching/Caching/Infrastructure/DistributedCacheService.cs` `TrySetAsync<T>` calls `GetAsync` instead of `SetAsync`.**
  Fixed: now calls `SetAsync(key, value, cancellationToken)`.
- [x] **Same file, `RemoveAsync` calls `_cache.RefreshAsync` instead of `_cache.RemoveAsync`.**
  Fixed: now calls `_cache.RemoveAsync(key, cancellationToken)`.
- [x] **`Serilog/Serilogger.cs` `EnsureInitialized` check is always true.**
  Fixed: replaced the broken type comparison with a `private static bool _initialized` flag.
- [x] **`SmtpMail/SmtpMail.cs` `SendAsync` never reads `mail.Attachments` — attachments are silently dropped.**
  Fixed: attachments are now added via `new Attachment(new MemoryStream(attachment.FileToBytes), attachment.FileName)`, matching `SmtpMailKit`.
- [x] **`ActiveDirectory/Services/LDAPService.cs`: `password.Trim()` throws `NullReferenceException` on null input; `LdapConnection` is never disposed.**
  Fixed: null check changed to `string.IsNullOrWhiteSpace(password)`; `LdapConnection` now created with `using`.
- [x] **Same file, `ChangePasswordAsync`: `DirectoryEntry`/`DirectorySearcher` are never disposed.**
  Fixed: `DirectoryEntry` (both the search root and the resolved user) and `DirectorySearcher` are now `using`-scoped.
- [x] **`FileGenerator/Infrastructure/Csv/ObjectConverter.cs` `DateTime.TryParse` uses the current thread culture instead of `CultureInfo.InvariantCulture`.**
  Fixed: now calls `DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out ...)`, matching `CsvService`. Note: the sibling `long`/`double`/`decimal`/`bool` parses just above it have the same current-culture exposure and were left as-is (not in original scope) — worth a follow-up if this matters for non-invariant-culture hosts.

## Maintainability

- [x] **`Caching/Caching/Infrastructure/MemoryCacheService.cs` `TrySetAsync` (both overloads) logs `"Cache {key} GET error"` for a Set path.**
  Fixed: both now log `"SET error"`.
- [x] **`ActiveDirectory/DependencyInjection.cs` has a dead commented-out line and two inconsistent options-registration patterns.**
  Fixed: removed the dead comment; both `AddActiveDirectory(Action<DomainOptions>)` and `AddLdapActiveDirectory` now consistently build the options instance manually and close over it, instead of one of them registering `IOptions<T>` via `services.Configure`. This also fixed a real bug: `AddLdapActiveDirectory` previously resolved `sp.GetRequiredService<LdapOptions>()`, but `services.Configure<T>` only registers `IOptions<T>` — that resolution would have thrown `InvalidOperationException` at runtime the first time `AddLdapActiveDirectory` was used.
- [x] **`Caching/Extensions/DependencyInjection/ServiceCollectionExtensions.cs`: private `AddDistributedCache` is never called.**
  Fixed: removed (dead code).
- [x] **`LDAPService.ChangePasswordAsync` is public but missing from `IActiveDirectoryService`, so it's unreachable through the DI-registered abstraction — and isn't actually asynchronous despite the `Async` suffix.**
  Fixed (breaking change): added `bool ChangePassword(string userName, string newPassword)` to `IActiveDirectoryService`; renamed `LDAPService.ChangePasswordAsync` → `ChangePassword`; implemented it on `ActiveDirectoryService` (via `UserPrincipal.SetPassword`/`Save`) and `FakeActiveDirectoryService` (returns `false`).
- [x] **`LDAPService.GetByUserNameAsync` throws `NotImplementedException`, silently breaking the `IActiveDirectoryService` contract for LDAP-based registration.**
  Fixed: documented the limitation with an XML `<remarks>` on the interface member.
- [x] **`SmtpMail.cs` / `SmtpMailKit.cs`: explicit `smtpClient.Dispose()` immediately before the end of a `using var` scope.**
  Fixed: removed the redundant manual `Dispose()` call in both.
- [x] **No shared interface for `SmtpMail`/`SmtpMailKit`, and their `SendAsync` signatures differ (one is missing `CancellationToken`).**
  Fixed (breaking change): added `ISmtpMailSender.SendAsync(MailFrom, MailMessage, CancellationToken = default)`, implemented by both `SmtpNetMailSender` (see rename below) and `SmtpMailKit`. `SmtpNetMailSender` now accepts a `CancellationToken` and wires it to `SmtpClient.SendAsyncCancel()` for cooperative cancellation (the legacy `System.Net.Mail.SmtpClient` has no token-accepting send overload on `netstandard2.1`).

## Naming

- [x] **`Light.SmtpMail.SmtpMail` duplicates its namespace name and reads oddly next to `SmtpMailKit`.**
  Fixed (breaking change): renamed class and file to `SmtpNetMailSender`. Updated the one call site (`tests/UnitTests/SmtpMailTests/SmtpMailTests.cs`).
- [x] **`IGraphTeams.GetByAsync(string user)` returns untyped `Task<object?>`.**
  Fixed (breaking change): renamed to `GetChatsAsync`, now returns `Task<Microsoft.Graph.Models.ChatCollectionResponse?>`. Updated `GraphTeamsService` and the sample `GraphController`.
- [x] **`FakeActiveDirectoryService`: unnecessary `await Task.Delay(1)` before returning `null`.**
  Fixed: now `Task.FromResult<DomainUserDto?>(default)`, no longer `async`.
- [x] **`Serilog/Startup.cs`: the `IHostBuilder` extension class is named `Startup`.**
  Fixed (breaking change): renamed class and file to `SerilogHostBuilderExtensions`. The one call site (`samples/WebApi/Program.cs`) uses extension-method syntax (`builder.Host.ConfigureSerilog()`) and needed no change.
- [x] **`ActiveDirectory`'s parameterless `AddActiveDirectory()` silently wires the Fake implementation with no signal to the consumer.**
  Partially addressed, non-breaking: added an XML doc comment on the overload explaining it registers the fake service and pointing to `AddActiveDirectory(Action<DomainOptions>)`/`AddLdapActiveDirectory` for a real backend. Did not rename/remove the overload or add `[Obsolete]`, to avoid forcing a call-site change for this specific item.

## Architecture & consistency (cross-cutting)

- [ ] **`SmtpMail` is the only one of the five service packages with no `ServiceCollectionExtensions`/DI registration at all.**
  Consumers must manually `new SmtpMail(...)`; no options pattern, breaking the convention the rest of the solution follows.
- [ ] **`Caching`: interfaces (`Extensions/Caching`, namespace `Light.Extensions.Caching`) live separately from their implementations (`Caching/Infrastructure`, namespace `Light.Caching.Infrastructure`).**
  Two disconnected namespace roots for one cohesive feature — reads as leftover structure. Consolidate under one namespace/folder.
- [ ] **Four different DI-registration conventions across the solution: `ServiceCollectionExtensions` (Caching/Graph/FileGenerator) vs a class literally named `DependencyInjection` (ActiveDirectory) vs `Startup` (Serilog) vs none (SmtpMail).**
  Pick one convention and apply it consistently.
- [ ] **Namespace/package mismatches:** `Mail.Contracts.csproj`'s actual namespace is `Light.Mail`, not `Light.Mail.Contracts`; `FileGenerator`'s namespaces are bare `Light.File.*`/`Light.Infrastructure.*` with no project-scoping segment (risks clashing with another package's own "Infrastructure" folder); `RootNamespace=Light` is set in three `.csproj` files but doesn't match the actual namespaces used, so it's unused/misleading metadata.
- [ ] **`Graph/Graph/...` and `Caching/Caching/...` repeat the project name as a folder name.**
  Minor but adds noise to every path; consider flattening (e.g. `Graph/Infrastructure`, `Graph/IGraphMailService.cs` directly under `src/Graph`).

## Dependencies & docs

- [ ] **`README.md` doesn't mention `Caching` or `Mail.Contracts`, even though both are real, shipped projects.**
  Update the project list.
- [ ] **`Graph.csproj` references `Microsoft.Extensions.Options.ConfigurationExtensions`, but no code in `src/Graph` binds configuration (`Bind`/`GetSection`/`IConfiguration`).**
  Appears unused — verify and remove.
- [ ] **`samples/WebApi/WebApi.csproj` references `Mapster`, but no `TypeAdapter`/`MapsterMapper` usage was found under `samples/WebApi`.**
  Appears unused — verify and remove.
- [ ] **Test coverage gap: `tests/UnitTests` only references and tests `SmtpMail`.**
  `ActiveDirectory`, `Caching`, `FileGenerator`, `Graph`, `Mail.Contracts`, and `Serilog` have zero unit test coverage in this solution.
