---
name: api
description: Playbook for designing or reviewing public API/contract surfaces using the api-designer agent, with explicit breaking-change awareness.
---

# Skill: API

## Purpose

Handle API design/review tasks (REST/gRPC contracts, public C# surface) for a specific solution/project, with explicit attention to backward compatibility since this repo is consumed by other solutions.

## Inputs

- The target solution/project and the specific API/contract in question.
- Whether this is new API design or a review of an existing/proposed change.

## Workflow

1. **Scope**: confirm the target solution/project and whether this is new design or review of a change.
2. **Delegate**: invoke [api-designer](../agents/api-designer.md) with the scoped contract/API.
3. **Classify changes**: for any change to existing API, explicitly classify as additive or breaking before proceeding.
4. **Check conventions**: compare against existing conventions in the same solution (naming, versioning, error shape) via `.claude/DEVELOPMENT.md` if populated.
5. **Report**: proposed/reviewed contract with rationale and explicit breaking-change flags.

## Expected Outputs

- A concrete contract design or review, with a clear breaking/additive classification for any change.
- Consistency notes relative to existing API conventions in the same solution.

## Best Practices

- Always flag breaking changes explicitly — never let one slip through as "just a small tweak."
- Don't design speculative future endpoints beyond what's requested.
- Match existing conventions in the same solution over generic REST/gRPC best practices when they conflict.
