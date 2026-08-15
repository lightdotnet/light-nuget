# ROT — Claude Config Rot Check

Periodic maintenance check for [agents/](agents/), [skills/](skills/), and [workflows/](workflows/): are they still a good fit for the **current Claude model/capabilities**, not whether the repo's own code has drifted.

This is not a documentation-sync workflow (see [sync-documentation](workflows/sync-documentation.md)) and does not touch `docs/generated/`, `PROJECT.md`, or `ARCHITECTURE.md`. It only looks at the `.claude/agents`, `.claude/skills`, `.claude/workflows` files themselves.

## Trigger

Run manually on request ("chạy ROT check", "run ROT", "check outdated agents/skills/workflows"). Not automatic — see [Scheduling](#scheduling) if the user wants a recurring cron run.

## Scope

Only these directories:

- `.claude/agents/*.md`
- `.claude/skills/*.md`
- `.claude/workflows/*.md`

Do not open unrelated solution/project code as part of this check.

## Checklist (per file)

For each agent/skill/workflow file, check:

1. **Model/version references** — does it name a specific Claude model, capability, or limitation that's since changed (e.g. a model version that's no longer current, a "Claude can't do X yet" note that's now stale, tool-call limits that changed)?
2. **Tool references** — does it assume tool names, parameters, or behaviors (e.g. `Read`, `Grep`, specific agent tool lists) that no longer match what's actually available?
3. **Overlap/redundancy** — does its "use for" description still carve out a distinct niche, or has it started overlapping with another agent/skill added later?
4. **Dead links** — do its markdown links to other `.claude/` files still resolve?
5. **Staleness signal** — no update despite the surrounding convention (agent roster, skill list, workflow index) having grown or changed materially since.

Mark each file: **OK**, **needs update** (say what), or **needs removal** (say why).

## Output

A short report, one row per file:

| File | Status | Notes |
|---|---|---|
| agents/xyz.md | needs update | still says "Claude Sonnet 4.5"; tool list mismatches current agent definition |

Do not edit the files during a ROT check unless the user asks — this is a report first, fix second.

## Log

Append one line per run so drift between runs is visible:

| Date | Run by | Result |
|---|---|---|
| 2026-08-13 | manual | first full run — 29/29 files OK, no findings |

## Scheduling

To automate a recurring run instead of triggering manually each time, use the `/schedule` skill to create a cron-based cloud agent that runs this checklist (e.g. weekly/monthly) and reports back — this file only defines *what* to check, not *when*.
