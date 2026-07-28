# Workflow: Implement Feature

Triggered by requests to add/implement new functionality.

## Steps

1. **Read relevant documentation.** Check `.claude/PROJECT.md`, `.claude/ARCHITECTURE.md`, `.claude/DEVELOPMENT.md` for the target solution/project — only the sections relevant to this feature's scope.
2. **Confirm scope.** Identify which solution/project the feature belongs in. Ask if ambiguous.
3. **Use appropriate agents** for design questions before planning:
   - [dotnet-architect](../agents/dotnet-architect.md) for structural/project-shape decisions.
   - [api-designer](../agents/api-designer.md) if new public API/contracts are involved.
   - [efcore-specialist](../agents/efcore-specialist.md) if data access/schema changes are involved.
4. **Produce an implementation plan**: files to add/change, approach, any public-API/breaking-change implications, and test strategy. Use plan mode for anything non-trivial.
5. **Wait for approval** before writing code. Do not start implementing during the planning step.
6. **Implement incrementally**: small, independently verifiable steps (build/tests passing at each step) rather than one large change.
7. **Test**: add/update tests for new behavior; optionally check coverage with [testing-reviewer](../agents/testing-reviewer.md).
8. **Update documentation only if requested.** Do not update `.claude/docs/generated/**` or `.claude/PROJECT.md`/`.claude/ARCHITECTURE.md` automatically — offer to, at the end (see [end-session](end-session.md)), but only act if the user says yes.

## Output

- An approved plan (surfaced to the user before implementation).
- Working, incrementally-built, tested code.
- A clear breaking-change flag if any public surface changed.
- Documentation updates only if explicitly requested.
