# Workflow: New Session

Run this at the start of every session in this repository.

## Steps

1. **Read `.claude/CLAUDE.md`.** This is the source of truth for operating rules, agent/skill/workflow index, and documentation rules. Do not skip even if this feels like a quick task.
2. **Do not read anything else yet.** Wait for the user's actual request before loading further context.
3. **Once the request is known, load only required context**:
   - If it names a solution/project/folder, jump straight there (`Glob` for the `.sln`/`.csproj`, don't tree-walk the repo).
   - If it's ambiguous which solution/project applies, ask rather than scanning broadly to figure it out.
   - Check `.claude/PROJECT.md`/`.claude/ARCHITECTURE.md`/`.claude/DEVELOPMENT.md` for already-verified facts about the relevant scope before re-deriving them from code.
4. **Identify relevant agents** from `.claude/CLAUDE.md` §4 that match the request's domain (architecture, EF Core, API, security, performance, testing, docs, dependencies) — plan to delegate rather than doing deep multi-domain analysis inline.
5. **Identify relevant skills/workflows** from `.claude/WORKFLOWS.md` and `.claude/CLAUDE.md` §9 (quick reference table) that match the request's shape.
6. **Read only the necessary files** for the identified scope — no speculative exploration.

## Output

Nothing to report yet — this workflow just establishes context before doing the actual requested work. Proceed directly into the matched skill/workflow/agent once identified.
