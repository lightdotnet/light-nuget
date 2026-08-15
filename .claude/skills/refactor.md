---
name: refactor
description: Playbook for safely refactoring scoped code in this shared framework repo without changing observable behavior or breaking consumers.
---

# Skill: Refactor

## Purpose

Restructure existing code for clarity/maintainability without changing behavior, with explicit care around this repo's role as a dependency for other solutions.

## Inputs

- The specific code/module to refactor and the motivation (e.g. duplication, unclear structure, outdated pattern).
- Confirmation of scope: one project, or does it touch a shared/common project consumed elsewhere?

## Workflow

1. **Confirm scope and motivation**: what's being refactored and why — avoid refactoring "while you're in there" beyond what was asked.
2. **Check public surface impact**: if the target is a shared/common project, determine whether any public type/member signatures would change. If yes, flag as a potential breaking change and confirm with the user before proceeding.
3. **Baseline behavior**: identify existing tests covering the target code; if coverage is thin, consider a quick pass with [testing-reviewer](../agents/testing-reviewer.md) before refactoring, so behavior changes are caught.
4. **Refactor incrementally**: small steps, each independently verifiable (build + tests pass) rather than one large rewrite.
5. **Verify no behavior change**: run existing tests; add characterization tests first if the area was undertested.
6. **Report**: summarize what changed structurally and confirm no observable behavior changed (or explicitly flag what did, if intentional).

## Expected Outputs

- Refactored code with unchanged observable behavior (unless explicitly agreed otherwise).
- A summary of the structural change and why.
- An explicit breaking-change flag if any public surface changed.

## Best Practices

- Never mix refactoring with new functionality in the same change — keep them separable.
- For shared/common projects, treat any public signature change as breaking until proven otherwise.
- Prefer the smallest refactor that achieves the stated goal; don't use a refactor request as license for a broader rewrite.
