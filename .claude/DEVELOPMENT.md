# Development Conventions

> Template. Fill in per-solution/per-project conventions only as they're actually observed in code (via `analyze-solution`/`analyze-project`), not from assumption. This file aggregates verified conventions; it is not a style guide invented up front.

## How to Use This File

- Each section should be scoped (which solution/project it applies to) unless a convention is verified to be truly repo-wide (e.g. an `.editorconfig` at the repo root, a shared `Directory.Build.props`).
- Do not add entries speculatively. If a convention hasn't been observed in actual code, leave the section marked `unknown`.

## Build & Tooling

- **Target framework(s)**: _unknown_
- **Shared build props/targets** (`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`): _unknown_
- **Central package management**: _unknown_
- **Solution/project structure convention**: _unknown_

## Coding Style

- **`.editorconfig` present**: _unknown_
- **Nullable reference types**: _unknown_
- **Naming conventions observed**: _unknown_
- **Formatting/analyzer rules**: _unknown_

## Project Layering Conventions

> Only document a layering convention once seen consistently across multiple projects — otherwise note it as local to one project.

- _unknown_

## Dependency Injection Patterns

- _unknown_

## Error Handling / Result Patterns

- _unknown_

## Logging Conventions

- _unknown_

## Testing Conventions

- **Test framework(s)** (all test projects, `net10.0`): NUnit 4 (`NUnit` 4.6.1, `NUnit3TestAdapter`, `NUnit.Analyzers`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`). Tests use `[Test]`/`[TestCase]`/`[SetUp]`; `NUnit.Framework` is a global using (`<Using Include="NUnit.Framework" />` or a `GlobalUsings.cs`). Test projects set `IsPackable=false`.
- **Assertion style**: NUnit constraint model — `Assert.That(actual, Is.EqualTo(...))`, `Assert.Multiple(...)`, `Assert.Throws<T>`/`ThrowsAsync<T>`. Several projects (`Specification.Tests`, `Caching.Tests`, framework `UnitTests`, plugins `UnitTests`) also have a local `LightAssert` class with fluent wrappers over `Assert.That` (`value.ShouldBe(x)`, `ShouldBeTrue()`, `ShouldNotBeNull()`, `ShouldHaveCount(n)`, ...); both styles coexist. No FluentAssertions/Shouldly packages.
- **Naming convention for test classes/methods**: `<TypeUnderTest>Tests` classes; methods mostly `Member_Scenario_ExpectedResult` (e.g. `AllowOrigins_Empty_Throws`); some older tests use sentence style (`Must_Send_Email_With_No_Exceptions`).
- **Mocking library**: `Moq` 4.20.72 is referenced by `Specification.Tests` and `EntityFrameworkCore.Tests`, but no test code currently uses it (no `using Moq`/`Mock<T>`). Tests use real implementations/hand-written fakes instead: EF Core InMemory provider (plus in-memory Sqlite for `SpecificationSqliteTests`), `ServiceCollection` containers, `DefaultHttpContext`.
- **Internals access**: `InternalsVisibleTo` is declared by `WebHost` (→ `WebHost.Tests`), `EventBus.MassTransit.RabbitMQ` (→ `EventBus.Tests`), and `SmtpMail`/`Serilog`/`ActiveDirectory` (→ plugins `UnitTests`).
- **Test project layout** (per solution, under `src/<solution>/tests/`):
  - `Framework.slnx`: one project per package — `Specification.Tests`, `EntityFrameworkCore.Tests` (also references `Specification.Tests`), `Caching.Tests`, `WebHost.Tests`; plus `UnitTests` covering `Extensions` and `SharedKernel` (folders `ExtensionsTests/`, `DomainTests/`). No test project for `Authorization`, `Modularity` or `Swagger`.
  - `Plugins.slnx`: a single `UnitTests` project referencing all five plugin packages, one folder per package (`ActiveDirectoryTests/`, `FileGeneratorTests/`, `GraphTests/`, `SerilogTests/`, `SmtpMailTests/`).
  - `EventBus.slnx`: `EventBus.Tests` (references both EventBus packages).
  - `BlazorComponents.slnx`: no test project.
- **Integration-test gating**: tests that need external systems are marked `[Category("Integration")]` and call `Assert.Ignore(...)` in `[SetUp]` unless env vars are set — `SmtpMailTests` requires `SMTP_PUBLIC_TEST=1`; `SmtpMailKitTests` requires `SMTP_TEST_USERNAME` and `SMTP_TEST_PASSWORD` (optional `SMTP_TEST_HOST`, default `smtp.ethereal.email`). Windows-only `ActiveDirectoryService` tests are ignored on non-Windows. No other env-var-gated tests were found (RabbitMQ/Elasticsearch/Graph tests run without a live service).

## EF Core Conventions

> See also [agents/efcore-specialist.md](agents/efcore-specialist.md), [skills/efcore.md](skills/efcore.md).

- **Migration strategy**: _unknown_
- **Configuration style** (Fluent API vs. attributes): _unknown_
- **Naming conventions for tables/columns**: _unknown_

## API Conventions

> See also [agents/api-designer.md](agents/api-designer.md), [skills/api.md](skills/api.md).

- **Versioning strategy**: _unknown_
- **Response/error contract shape**: _unknown_

## Versioning & Release

- **Package versioning scheme** (per solution):
  - `Framework` and `Plugins`: a single `<NugetVersion>` in `src/framework/Directory.Build.props` / `src/plugins/Directory.Build.props` (currently `2.0.2-preview.1` in both). Each package has its own alias property defaulting to it (e.g. `<CachingVersion>$(NugetVersion)</CachingVersion>`, `<SerilogVersion>$(NugetVersion)</SerilogVersion>`), and each `.csproj` sets `<Version>$(<Package>Version)</Version>` — so all packages of a solution share one version unless an alias is overridden.
  - `EventBus`: no version in `Directory.Build.props`; `<Version>` is set directly in each `.csproj` (`EventBus.csproj` and `EventBus.MassTransit.RabbitMQ.csproj`, currently `0.2.1`).
  - `BlazorComponents`: `<Version>` is set directly in each `.csproj` (independent values).
  - Package id = `AssemblyName` (`$(OrgName).<Name>` / `Lightsoft.*`; no explicit `PackageId`); all packable projects set `GeneratePackageOnBuild=True`.
- **Changelog convention**: no CHANGELOG file and no populated `<PackageReleaseNotes>` (empty where present). Breaking/behavior changes are recorded in each package's README under a `## Breaking / behavior changes` → `### Unreleased` section (present in `src/eventbus/README.md` and the five plugin READMEs). Framework package READMEs record such changes inline in their "Notes" sections (e.g. "Behavior change (...)" bullets in the WebHost README) rather than in an `Unreleased` section.

---
_Last updated: 2026-09-29 — only "Testing Conventions" and "Versioning & Release" populated (verified against code); all other sections still unpopulated._
