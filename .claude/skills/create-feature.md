---
name: create-feature
description: Playbook for adding new functionality to a scoped project/solution in this shared framework repo, from plan to incremental implementation.
---

# Skill: Create Feature

## Purpose

Guide the addition of new functionality to a specific project/solution without assuming repo-wide impact, and without skipping planning for anything non-trivial.

## Inputs

- A description of the desired feature/behavior.
- The target solution/project (ask if not specified — never guess across a multi-solution repo).
- Any relevant existing documentation (`.claude/PROJECT.md`, `.claude/ARCHITECTURE.md`, `.claude/DEVELOPMENT.md`) for the target scope.

## Workflow

1. **Scope**: confirm which solution/project the feature belongs in. If it could plausibly belong in a shared/common project vs. a specific one, ask.
2. **Read minimally**: read only the files/projects directly relevant (target project, its direct dependencies, existing similar features as reference).
3. **Delegate design questions**: use [dotnet-architect](../agents/dotnet-architect.md) for structural decisions, [api-designer](../agents/api-designer.md) if public API is involved, [efcore-specialist](../agents/efcore-specialist.md) if data access is involved.
4. **Produce a plan**: outline the approach, files to add/change, and any public API/breaking-change implications. Present it before writing code (see [workflows/implement-feature.md](../workflows/implement-feature.md)).
5. **Wait for approval** before implementing.
6. **Implement incrementally**: small, reviewable steps rather than one large change.
7. **Test**: add/adjust tests for the new behavior; consider using [testing-reviewer](../agents/testing-reviewer.md) to check coverage of edge cases.
8. **Docs**: update documentation only if the user requests it (see [sync-docs](sync-docs.md)).

## Expected Outputs

- An approved implementation plan.
- Working, tested code changes scoped to the target project(s).
- A clear note of any public API surface added/changed, and whether it's a breaking change for consumers.

## Best Practices

- Don't introduce a new shared abstraction unless the feature genuinely needs to be reused elsewhere — see repo-wide guidance against premature abstraction.
- Keep the change scoped to the target solution; if it turns out to require touching another solution, stop and confirm with the user first.
- Prefer extending existing patterns in the target project over inventing new ones.
