# Architecture

> Template. Populated incrementally per solution/project, only when explicitly requested (see [skills/review-architecture.md](skills/review-architecture.md), [agents/architecture-reviewer.md](agents/architecture-reviewer.md)). Do not assume a single architecture applies repo-wide — this repository may host several independently-architected solutions.

## Scope of This Document

This file aggregates **verified** architectural facts only. Each section should note which solution/project it applies to — never assume it generalizes to the whole repo.

## Layering

> Per solution. Only document layers actually observed (e.g. Domain / Application / Infrastructure / API), not an assumed template.

| Solution | Layers | Notes |
|---|---|---|
| _unknown_ | | |

## Dependency Direction

> Verified via project references, not inferred from folder names.

- _unknown_

## Shared Kernel / Common Building Blocks

> E.g. base entities, shared abstractions, common EF Core conventions — only list what's confirmed in code, with file references.

- _unknown_

## Data Access

> Per solution: ORM, DbContext(s), migration strategy. Do not assume a single DbContext exists repo-wide.

| Solution | DbContext(s) | Provider | Notes |
|---|---|---|---|
| _unknown_ | | | |

## API Surfaces

> Per solution: REST/gRPC/other, versioning strategy, hosting model.

| Solution | API type | Versioning | Notes |
|---|---|---|---|
| _unknown_ | | | |

## Cross-Solution Coupling

> Only document actual references between solutions/projects in this repo, verified via `.csproj`/`.sln` inspection — never assumed.

- _unknown_

## Architectural Risks / Debt

> Findings from `review-architecture` runs go here, tagged with date and scope.

- _unknown_

---
_Last updated: never (template not yet populated)_
