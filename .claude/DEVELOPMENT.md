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

- **Test framework(s)**: _unknown_
- **Naming convention for test classes/methods**: _unknown_
- **Mocking library**: _unknown_
- **Test project layout**: _unknown_

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

- **Package versioning scheme**: _unknown_
- **Changelog convention**: _unknown_

---
_Last updated: never (template not yet populated)_
