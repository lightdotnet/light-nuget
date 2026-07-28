# Workflow: Analyze Solution

Triggered by requests like "analyze this solution" naming a specific `.sln`/solution.

## Steps

1. **Confirm the exact solution.** This repo may contain multiple solutions — ask if it's not clear which one is meant.
2. **Analyze only that solution.** Use [analyze-solution skill](../skills/analyze-solution.md) to enumerate its projects and map dependencies.
3. **Understand dependencies**: delegate to [dependency-analyzer](../agents/dependency-analyzer.md) for the project/package reference graph within the solution; note (but don't chase) any references to projects outside the solution.
4. **Optionally assess structure**: if the user also wants a structural/architecture read, delegate to [architecture-reviewer](../agents/architecture-reviewer.md).
5. **Update documentation only if requested**:
   - Update the solution's row in `.claude/PROJECT.md`.
   - Write/update `.claude/docs/generated/<solution>/overview.md` using [solution-overview template](../docs/templates/solution-overview.md).
   - If not asked, report findings conversationally without writing files.

## Output

- A description of the solution's projects, responsibilities, and dependency graph.
- Optionally, updated `.claude/PROJECT.md` entries and/or a generated solution overview doc.
