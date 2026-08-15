# Workflow: End Session

Run at the end of a non-trivial session (meaningful code/doc changes were made).

## Steps

1. **Summarize completed work.** A concise account of what changed, scoped to what actually happened this session — no restating the whole conversation.
2. **Suggest documentation updates.** If code changed in ways that would make `.claude/PROJECT.md`, `.claude/ARCHITECTURE.md`, `.claude/DEVELOPMENT.md`, or a generated doc under `.claude/docs/generated/` stale, name specifically which doc(s) and why.
3. **List follow-up tasks.** Anything identified but not done this session (e.g. a review finding not yet fixed, a test gap noted but not closed, a breaking-change flag that needs a decision).
4. **Do not automatically modify documentation.** Suggesting is the end of this workflow's responsibility — actually syncing requires the user to explicitly invoke [sync-documentation](sync-documentation.md) in a future step/session.

## Output

- A short summary of completed work.
- A list of suggested documentation updates (not applied).
- A list of follow-up tasks/open questions.
