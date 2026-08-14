[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# UnitTests

NUnit test project for the `IntegrationServices` solution (`src/services`). Targets **net10.0** (see `TargetFramework` in `UnitTests.csproj`; a stale `obj/Debug/net9.0` build output folder exists on disk but is not what the project currently targets).

Uses `NUnit` 4.6.1 + `NUnit3TestAdapter` 6.2.0 + `Microsoft.NET.Test.Sdk` 18.6.0, with `NUnit.Analyzers` and `coverlet.collector` for coverage collection. `GlobalUsings.cs` globally imports `NUnit.Framework`.

## What's referenced

`UnitTests.csproj` has a `ProjectReference` to every library project in the solution: `ActiveDirectory`, `FileGenerator`, `Graph`, `Serilog`, `SmtpMail`. It also explicitly references `Microsoft.Extensions.Configuration`/`Microsoft.Extensions.Configuration.Binder` (for the Serilog config-binding tests) and `Microsoft.Extensions.DependencyInjection` (for the `ServiceCollection`/`BuildServiceProvider` DI-registration tests) — both are otherwise only pulled in transitively.

## What's covered

### `SmtpMailTests/SmtpMailTests.cs`

Exercises `Light.Smtp.SmtpNetMailSender.SendAsync`. One test, `Must_Send_Email_With_No_Exceptions`:
- Constructs a `SmtpNetMailSender` against host `smtp.freesmtpservers.com` with `UseSsl = false`.
- Calls `SendAsync(from, fromDisplayName, recipients, subject, content)` (from/to `user@domain.local`) and passes if it completes without throwing — there is no assertion beyond "did not throw."

### `SmtpMailTests/SmtpMailKitTests.cs`

Exercises `Light.Smtp.SmtpMailKitSender.SendAsync`. One test, `Must_Send_Email_With_No_Exceptions`:
- Constructs a `SmtpMailKitSender` against host `smtp.ethereal.email` with a hardcoded username `waino.kuhlman@ethereal.email` and password `RUMp811zYYVkPuvcdY`, `UseSsl = false`.
- Calls `SendAsync(from, fromDisplayName, recipients, subject, content)` (from `waino.kuhlman@ethereal.email`, to `user@domain.local`) and passes if it completes without throwing.
- The constructor carries a doc comment: "Please config new ethereal before Tests" — i.e. these credentials are expected to need periodic refreshing against ethereal.email, and the test will start failing once they expire/rotate.

**Important — these are not mocked.** Both tests perform real network I/O against live, external, third-party test SMTP servers (`smtp.freesmtpservers.com` and `smtp.ethereal.email`). Consequences:
- They are **flaky and slow** relative to typical unit tests — pass/fail depends on those external services being reachable and healthy at the time the suite runs.
- The `smtp.ethereal.email` test additionally depends on the hardcoded credentials above still being valid; when ethereal.email rotates/expires them, the test will fail until someone updates `SmtpMailKitTests.cs` with fresh credentials.
- Neither test cleans up or verifies delivery — they only assert that `SendAsync` didn't throw, not that the mail actually arrived.

### `LightAssert.cs`

A small internal extension-method helper (`namespace UnitTests`) wrapping common `NUnit.Framework.Assert.That` calls for more fluent/readable call sites:
- `ShouldBe<T>(this T value, T equalTo)` → `Assert.That(value, Is.EqualTo(equalTo))`
- `ShouldNotBeNullOrEmpty<T>(this T value)` → `Assert.That(value, Is.Not.Null.And.Not.Empty)`
- `ShouldContains<T>(this IEnumerable<T> value, object obj)` → `Assert.That(value, Contains.Item(obj))`
- `ShouldBeTrue(this bool value)` / `ShouldBeFalse(this bool value)` → thin wrappers over `ShouldBe(value, true/false)`

It adds no behavior beyond NUnit's built-in constraint model — it's purely fluent sugar. It's used by the newer test suites below (`ShouldBe`, `ShouldBeTrue`/`ShouldBeFalse`); the original `SmtpMailTests`/`SmtpMailKitTests` still don't use it.

### `FileGeneratorTests/` (`FileGenerator`)

`CsvServiceTests` and `ExcelServiceTests` exercise `CsvService`/`ExcelService` fully in-memory (`MemoryStream`/`StringReader`, no disk I/O). Beyond basic read/write round-trips, these pin down several real behavioral quirks as regression tests:
- `ObjectConverter`'s type-detection order means an `object`-typed CSV value comes back as `double`, never `decimal`.
- The non-generic `Read(...)` overload never actually applies `ObjectConverter` — values are always raw strings.
- `IExcelService.ReadAs<T>` returns an empty sequence for a missing sheet, while `ReadAsDataTable`/`ReadAsObjects` throw for the same input.
- `ReadAsObjects` throws `FormatException` for a fractional numeric cell (via `Convert.ToInt64`) rather than rounding/truncating it — this corrects an earlier, incorrect assumption in `FileGenerator/README.md` that it silently rounds; a test proved otherwise and the README was fixed to match.
- CSV date parsing is invariant-culture regardless of the running thread's culture.

### `SerilogTests/` (`Serilog`)

`SerilogOptionsExtensionsTests` builds an in-memory `IConfiguration` (`ConfigurationBuilder().AddInMemoryCollection(...)`) to test `SerilogOptionsExtensions.GetWriteTo`/`GetWriteToOptions` in isolation — including a regression test pinning the exact `"ElasticsearchAsync1"` vs `"ElasticsearchAsync"` config-key bug found in `samples/WebApi/appsettings.json` (see the repo `TODO.md`), and confirming the name match is case-sensitive.

`SerilogLoggerTests` tests `Serilogger.EnsureInitialized()`'s idempotency by asserting `Log.Logger` doesn't get reassigned across repeated calls (reference-equality, not initial-state — `_initialized` is a shared `static` field, so asserting its pre-call state would be test-order-dependent and unsafe). This is a regression test for the always-reinitializing bug fixed earlier this session.

Not covered, deliberately: `SerilogConfigurationExtensions`/`SerilogHostBuilderExtensions` — these need a real `IHostBuilder`/hosting context to exercise meaningfully, and their private `WriteToFile`/`WriteToElasticsearch` helpers would require real disk/Elasticsearch I/O to test past the point of just re-testing Serilog's own sink libraries.

### `ActiveDirectoryTests/` (`ActiveDirectory`)

Covers everything in this project that doesn't require a live AD/LDAP server: `FakeActiveDirectoryService` (all four `IActiveDirectoryService` members), `DomainOptions`/`LdapOptions` default values (including `DomainOptions.Enable`'s actual — not `IsNullOrWhiteSpace` — null-check semantics), `DomainUserDto` construction and record equality, and `AddActiveDirectory()`'s DI registration (resolves as `FakeActiveDirectoryService`, transient lifetime).

Not covered, deliberately: `ActiveDirectoryService` (`System.DirectoryServices.AccountManagement`) and `LDAPService` (`Novell.Directory.Ldap`) both construct real connections to a Domain Controller/LDAP server directly inside their methods — there's no injectable seam to fake, and mocking `PrincipalContext`/`LdapConnection`/`DirectoryEntry` themselves isn't practical. Testing either meaningfully needs a real AD/LDAP server, out of scope here. `AddActiveDirectory(Action<DomainOptions>)` and `AddLdapActiveDirectory` are also untested beyond what's implied above, for the same reason.

### `GraphTests/` (`Graph`)

`ServiceCollectionExtensionsTests` covers `AddMicrosoftGraph`'s DI registration only: `GraphServiceClient` as singleton, `IGraphMailService` as scoped, `IGraphTeams` as transient, the options-`Action` being invoked exactly once, and the fluent `IServiceCollection` return. This is safe without a real Azure AD tenant because constructing `ClientSecretCredential`/`GraphServiceClient` doesn't itself perform any token/network request — only calling a Graph API through them does.

Not covered, deliberately: `GraphMailService`/`GraphTeamsService` themselves — `GraphServiceClient`'s fluent request builders (e.g. `.Users[x].Chats`, `.Users[x].SendMail`) are concrete generated types built from an internal `RequestAdapter`, not interfaces, so there's no reasonable seam to substitute a fake response without disproportionate scaffolding (a fake `IRequestAdapter` plus hand-built serialized responses per call). `GraphOptions` itself is a plain property bag with no logic, not worth testing.

## Coverage gap

`ActiveDirectoryService`, `LDAPService`, `GraphMailService`, `GraphTeamsService`, and Serilog's host-builder/config-application code remain untested, for the live-infra/no-seam reasons detailed above. If any of these need coverage later, the realistic options are: a real (or containerized) test LDAP/AD server, a dedicated Azure AD test tenant, or wrapping the Graph SDK's request builders behind a thin interface seam introduced specifically to make them fakeable — none of which is a small addition.

## Running the tests

From this folder:

```powershell
dotnet test
```

Or from the solution root (`src/services`):

```powershell
dotnet test IntegrationServices.slnx
```

Because `SmtpMailTests` and `SmtpMailKitTests` hit live external SMTP servers, expect occasional failures unrelated to code changes (network/service availability, or expired ethereal.email credentials) rather than assuming a failing run always indicates a regression.
