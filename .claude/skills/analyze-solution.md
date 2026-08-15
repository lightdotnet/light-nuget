---
name: analyze-solution
description: Playbook for analyzing a single named solution (.sln) — its projects, dependencies, and structure — without touching sibling solutions.
---

# Skill: Analyze Solution

## Purpose

Build (or refresh) an understanding of exactly one solution in this multi-solution repo: its projects, how they relate, and what it's for. Never assumes findings generalize to other solutions in the repo.

## Inputs

- The specific `.sln` file or solution name to analyze (ask if not specified — do not guess which solution the user means in a multi-solution repo).

## Workflow

1. **Locate the solution**: find the `.sln` file and enumerate the projects it actually includes.
2. **Map dependencies**: delegate to [dependency-analyzer](../agents/dependency-analyzer.md) to build the real project/package dependency graph for this solution.
3. **Understand structure**: delegate to [architecture-reviewer](../agents/architecture-reviewer.md) if a structural/layering assessment is also wanted; otherwise just describe what's observed.
4. **Read minimally**: open only the files needed to describe each project's responsibility (e.g. namespaces, key public types) — not every file in every project.
5. **Update docs, if requested**: if the user wants this persisted, write/update `.claude/PROJECT.md` (solution table) and/or a scoped file under `.claude/docs/generated/<solution>/overview.md` using the [solution-overview template](../docs/templates/solution-overview.md). Do this only when explicitly asked.

## Expected Outputs

- A description of the solution: its projects, their responsibilities, and how they depend on each other.
- A dependency graph/table for the solution.
- Optionally, updated documentation (only if requested).

## Best Practices

- Stay within the named solution; note (but don't chase) references to projects outside it.
- Don't assume this solution's conventions apply to other solutions in the repo.
- Prefer delegating deep dependency/architecture work to the relevant agent rather than doing it all inline.
