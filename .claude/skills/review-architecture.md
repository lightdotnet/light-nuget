---
name: review-architecture
description: Playbook for reviewing layering, boundaries, and structural cohesion of a scoped solution/project using the architecture-reviewer agent.
---

# Skill: Review Architecture

## Purpose

Assess the structural health of a specific solution, project, or folder — layering, dependency direction, boundary cohesion — without assuming a repo-wide architecture exists.

## Inputs

- The target scope: a solution, project, or folder (ask if not specified).
- Existing `.claude/ARCHITECTURE.md` content for that scope, if any, as a baseline to compare against.

## Workflow

1. **Scope**: confirm the exact solution/project/folder to review.
2. **Delegate**: invoke [architecture-reviewer](../agents/architecture-reviewer.md) with that scope.
3. **Verify, don't assume**: the agent should build its dependency picture from actual `.csproj`/`.sln` references, not folder-name conventions.
4. **Report**: present findings ranked by severity, each tied to a concrete file/reference.
5. **Optionally persist**: if the user asks to update documentation with the findings, hand off to [sync-docs](sync-docs.md) — do not update `.claude/ARCHITECTURE.md` automatically.

## Expected Outputs

- A scoped architectural assessment: dependency direction, layering observations, boundary issues.
- Prioritized findings with rationale and suggested fixes.
- Explicit note of anything out of scope that would need a follow-up review.

## Best Practices

- Never expand the review to sibling solutions without flagging it first.
- Don't propose a full re-architecture unless asked; report findings and let the user decide.
- This is a read-only skill — no code changes.
