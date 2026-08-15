---
name: analyze-project
description: Playbook for analyzing a single named project (.csproj) — its responsibility, public surface, and dependencies — without expanding scope to its whole solution.
---

# Skill: Analyze Project

## Purpose

Build (or refresh) an understanding of exactly one project: what it's responsible for, what it exposes publicly, and what it depends on. Narrower than [analyze-solution](analyze-solution.md).

## Inputs

- The specific `.csproj` or project name to analyze (ask if ambiguous — multiple projects may share a similar name across solutions).

## Workflow

1. **Locate the project**: find the `.csproj` file and its containing folder.
2. **Read its references**: `<ProjectReference>` and `<PackageReference>` entries — delegate to [dependency-analyzer](../agents/dependency-analyzer.md) if the graph is non-trivial.
3. **Read its public surface**: enumerate public types/members at a summary level (don't paste full file contents unless asked).
4. **Identify responsibility**: infer the project's purpose from its actual contents (namespaces, key types), not its name alone.
5. **Update docs, if requested**: write/update a scoped entry in `.claude/PROJECT.md` (project table) and/or `.claude/docs/generated/<solution>/<project>/overview.md` using the [project-overview template](../docs/templates/project-overview.md). Only when explicitly asked.

## Expected Outputs

- A description of the project's responsibility, public surface, and dependencies.
- Optionally, updated documentation (only if requested).

## Best Practices

- Stay within the named project; note references to other projects but don't fully analyze them unless asked.
- Summarize public surface rather than reproducing full source.
- Don't assume the project's role from its name — verify against actual contents.
