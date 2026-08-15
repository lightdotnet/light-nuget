# AI Working Rules

Detailed operating constraints for Claude when working in this repository. `CLAUDE.md` is the authoritative summary; this file expands on the reasoning and edge cases.

## Repository Shape Assumptions

Always assume, until verified otherwise for the specific task at hand:

- **Multiple solutions** may exist side by side. A `.sln` file's presence doesn't mean it's "the" solution.
- **Multiple `DbContext` types** may exist across projects. Never assume there's one.
- **Multiple API surfaces** (or none) may exist. Some projects may be pure libraries with no host at all.
- **No single "startup project."** Don't look for `Program.cs` at the repo root and assume it's the entry point for everything.
- Projects may target different TFMs (target framework monikers), different C# language versions, and different conventions. Don't normalize across them without checking.

## Context-Loading Strategy

1. **Task scoping first.** Before reading code, identify the minimum solution/project/folder the task actually touches. If ambiguous, ask the user rather than reading broadly to disambiguate yourself.
2. **Index before content.** Use `Glob`/`Grep` to locate relevant `.csproj`/`.sln`/namespaces before opening files. Don't open files "to see what's there."
3. **Read incrementally.** Open only the files needed for the current step of the task. Re-scope and read more only when a genuine dependency is found (e.g., a referenced project, a base class in another folder).
4. **No repo-wide scans without an explicit request.** "Analyze this folder" means that folder. "Analyze this solution" means that solution and its referenced projects — not sibling solutions.
5. **Cache findings in the right place.** Verified facts about structure go into `PROJECT.md`/`ARCHITECTURE.md` (only when a sync/generate step is explicitly requested) — not into ad hoc notes that vanish at session end.

## Token Efficiency Rules

- Prefer `Grep -n` targeted excerpts over full-file reads when only a symbol or pattern is needed.
- Prefer delegating multi-file investigations to a specialized agent (see `CLAUDE.md` §4) so exploration detail stays out of the main context — the agent reports a summary back.
- Never paste an entire large generated file (migrations, designer files, `obj/`/`bin/` artifacts) into the conversation.
- Summarize diffs and findings in prose/tables rather than reproducing full file contents when reporting back to the user.
- Avoid redundant re-reads: if a file was already read this session and hasn't been edited since, don't re-read it "to be sure."

## When to Delegate vs. Do Inline

Delegate to a specialized agent when:

- The task maps directly to one agent's domain (architecture, EF Core, API design, security, performance, testing, docs, dependencies).
- The investigation would require reading many files whose contents don't need to stay in the main context.
- A second, independent opinion is valuable (e.g. security or architecture review).

Do inline when:

- The task is a small, well-scoped edit in a file already open/known.
- The user is mid-conversation about a specific line/function and wants a direct answer.

## Documentation Discipline

- Never regenerate `docs/generated/**` or rewrite `PROJECT.md`/`ARCHITECTURE.md` sections as a side effect of an unrelated task.
- When a sync is requested, follow [workflows/sync-documentation.md](workflows/sync-documentation.md) exactly: diff against code, update changed sections, remove stale ones, preserve manually-authored content.
- Templates in `docs/templates/` are structural skeletons — copy their structure into `docs/generated/` outputs, don't edit the templates themselves during normal doc generation.

## Multi-Solution Safety Rules

- Before making a change that could affect more than one solution/project, check actual project references — don't assume isolation or assume coupling.
- Public API changes (public types/members, NuGet package surface) are potential breaking changes for out-of-repo consumers. Flag this explicitly to the user before proceeding.
- Cross-solution refactors require explicit user confirmation before starting (see `CLAUDE.md` §2.7).
