# Workflow: Review Repository

Triggered by broad, read-only review requests ("review the codebase," "audit this repo/solution").

## Steps

1. **Confirm scope.** Even a "repository review" should be scoped to what's practical and relevant — confirm whether the user means one solution, a set of solutions, or truly the entire repo (which may be large and multi-solution; consider proposing a per-solution pass instead of one giant scan).
2. **Use multiple agents**, each covering its domain, over the confirmed scope:
   - [architecture-reviewer](../agents/architecture-reviewer.md)
   - [code-reviewer](../agents/code-reviewer.md)
   - [security-reviewer](../agents/security-reviewer.md)
   - [performance-reviewer](../agents/performance-reviewer.md)
   - [testing-reviewer](../agents/testing-reviewer.md)
   - [dependency-analyzer](../agents/dependency-analyzer.md)
   - Add [efcore-specialist](../agents/efcore-specialist.md) / [api-designer](../agents/api-designer.md) if the scope includes data access / public APIs.
3. **Produce prioritized findings**: merge each agent's output into one report, ordered by severity/impact across domains (e.g. security/correctness issues before style nits).
4. **Do not modify code.** This workflow is strictly read-only/advisory — findings are reported, not applied.
5. **Offer next steps**: suggest which findings might warrant a follow-up (e.g. [refactor](../skills/refactor.md), [implement-feature](implement-feature.md) for fixes) without applying them automatically.

## Output

- A single, prioritized findings report spanning the agents used, each finding attributed to its domain with file references.
- No code changes.
- Suggested follow-up actions, left for the user to approve.
