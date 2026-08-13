# CLAUDE.md — Primary Source of Truth

This file is the entry point for every Claude Code session in this repository. Read it first, every session, before touching anything else.

## 1. Repository Purpose

This repository is a **reusable C#/.NET shared framework/library**, not a single application. It is consumed by multiple, independent downstream .NET solutions.

Consequences of this:

- There is **no single startup project**, no single architecture, no single `DbContext`, no single API surface, and no single "the app."
- The repo may contain multiple solutions, each with its own projects, layering, and conventions.
- Changes here can ripple into consumers that are not present in this repository. Treat public surface area (public types, members, NuGet package contents) as a contract.
- Documentation and analysis must be **incremental and scoped** — per folder, per project, or per solution — never assumed to apply repo-wide unless explicitly verified.

## 2. AI Operating Rules

1. **Read only what the current task needs.** Do not open files, folders, or solutions unrelated to the current request. Prefer `Glob`/`Grep` targeted lookups over broad tree walks.
2. **Prefer specialized agents over doing everything inline.** See [Agent Usage](#4-agent-usage) — architecture, EF Core, API design, security, performance, testing, docs, and dependency questions each have a dedicated agent. Delegate to them rather than reasoning about all domains yourself in the main context.
3. **Minimize token usage.** Summarize instead of pasting large file contents back to the user. Avoid re-reading files already read this session. Avoid speculative exploration "just in case."
4. **Repository analysis is incremental, never automatic.** Do not proactively scan or document the whole repository, a whole solution, or even a whole project unless the user explicitly asks. A request about one folder is a request about that folder only.
5. **Documentation synchronization only happens on request.** Never regenerate or rewrite files under `.claude/docs/generated/` (or any `AI_CONTEXT.md`/`PROJECT.md`/`ARCHITECTURE.md` content) unless the user explicitly asks to generate or sync docs.
6. **Ask before assuming structure.** If it's unclear which solution/project/folder a request applies to, ask rather than guessing across the whole repo.
7. **No destructive or repo-wide edits without confirmation.** Multi-solution refactors, mass renames, or dependency bumps that touch more than one solution require explicit user confirmation first.

## 3. Documentation Rules

- `.claude/PROJECT.md`, `.claude/ARCHITECTURE.md` are **living templates** — they start mostly empty and are filled in incrementally as solutions/projects are analyzed, and only on request.
- `.claude/docs/templates/` holds **unpopulated templates** for generated docs (solution overview, project overview, API docs, DB docs, domain model, conventions, dependency graph, dev guide). Never delete or repurpose these.
- `.claude/docs/generated/` holds **actual generated documentation**, organized by solution/project (e.g. `docs/generated/<SolutionName>/overview.md`). Only written to when a sync/generate workflow is explicitly invoked.
- Manually-authored documentation (anything a human wrote and didn't come from a generate/sync workflow) must be preserved during sync — never silently overwritten. See [sync-documentation workflow](workflows/sync-documentation.md).
- Outdated generated content should be removed during an explicit sync, not left to drift silently.

## 4. Agent Usage

Specialized agents live in [agents/](agents/). Prefer delegating to them over reasoning inline:

| Agent | Use for |
|---|---|
| [architecture-reviewer](agents/architecture-reviewer.md) | Layering, boundaries, dependency direction, module cohesion |
| [code-reviewer](agents/code-reviewer.md) | General code quality, correctness, maintainability review |
| [dotnet-architect](agents/dotnet-architect.md) | .NET/C# design decisions, project structure, framework choices |
| [efcore-specialist](agents/efcore-specialist.md) | EF Core models, migrations, query performance, DbContext design |
| [api-designer](agents/api-designer.md) | Public API surface, REST/gRPC contracts, versioning |
| [security-reviewer](agents/security-reviewer.md) | Vulnerabilities, secrets, auth, unsafe patterns |
| [performance-reviewer](agents/performance-reviewer.md) | Hot paths, allocations, async misuse, query performance |
| [testing-reviewer](agents/testing-reviewer.md) | Test coverage, test quality, missing edge cases |
| [documentation-writer](agents/documentation-writer.md) | Generating/updating docs from code |
| [dependency-analyzer](agents/dependency-analyzer.md) | Project/package references, dependency graphs, coupling |

Rule of thumb: if a task maps cleanly to one row above, delegate to that agent instead of doing the analysis in the main thread.

## 5. Skill Usage

Reusable playbooks live in [skills/](skills/) — see each file for purpose, inputs, workflow, and best practices:

- [create-feature](skills/create-feature.md), [refactor](skills/refactor.md)
- [review-code](skills/review-code.md), [review-architecture](skills/review-architecture.md)
- [generate-docs](skills/generate-docs.md), [sync-docs](skills/sync-docs.md)
- [analyze-solution](skills/analyze-solution.md), [analyze-project](skills/analyze-project.md)
- [efcore](skills/efcore.md), [api](skills/api.md), [testing](skills/testing.md), [performance](skills/performance.md)

## 6. Workflow Usage

Session- and task-level workflows live in [workflows/](workflows/) — see [WORKFLOWS.md](WORKFLOWS.md) for the index:

- [new-session](workflows/new-session.md) — start of every session
- [analyze-folder](workflows/analyze-folder.md), [analyze-solution](workflows/analyze-solution.md)
- [implement-feature](workflows/implement-feature.md)
- [review-repository](workflows/review-repository.md)
- [sync-documentation](workflows/sync-documentation.md)
- [end-session](workflows/end-session.md) — end of every non-trivial session
- [ROT.md](ROT.md) — checks agents/skills/workflows for staleness against the current Claude model/capabilities; manual trigger only, never automatic

## 7. Context Management

- Load context in this order: `CLAUDE.md` → the specific doc/template relevant to the task → the specific code files needed. Never load all of `docs/generated/` at once.
- Prefer `Grep`/`Glob` to locate the relevant solution/project before reading files.
- When a task is scoped to one solution or folder, do not read sibling solutions/folders "for context" unless a real dependency exists (verified via project references, not assumption).
- Summarize large findings; don't paste entire files into the conversation when a targeted excerpt will do.
- See [AI_CONTEXT.md](AI_CONTEXT.md) for detailed working rules.

## 8. Documentation Synchronization Rules

- Sync is **explicit and pull-based**: it happens only when the user runs a sync/generate request (see [sync-documentation](workflows/sync-documentation.md), [sync-docs skill](skills/sync-docs.md)).
- Sync must diff current generated docs against current code, update what changed, remove what's stale, and leave manually-authored sections untouched.
- Never sync as a side effect of an unrelated task (e.g., don't "helpfully" update docs while implementing a feature unless asked).

## 9. Quick Reference for Common Commands

| User says | Do this |
|---|---|
| "Analyze this solution" | [workflows/analyze-solution.md](workflows/analyze-solution.md) |
| "Analyze this project" | [skills/analyze-project.md](skills/analyze-project.md) |
| "Analyze this folder" | [workflows/analyze-folder.md](workflows/analyze-folder.md) |
| "Generate documentation" | [skills/generate-docs.md](skills/generate-docs.md) |
| "Sync documentation" | [workflows/sync-documentation.md](workflows/sync-documentation.md) |
| "Review architecture" | [skills/review-architecture.md](skills/review-architecture.md) |
| "Review code" | [skills/review-code.md](skills/review-code.md) |
| "Implement a feature" | [workflows/implement-feature.md](workflows/implement-feature.md) |
| "Update CLAUDE documentation" | [skills/sync-docs.md](skills/sync-docs.md), scoped to `.claude/` docs only |
| "Run ROT check" / "check outdated agents/skills/workflows" | [ROT.md](ROT.md) — manual trigger only |
