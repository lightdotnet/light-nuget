---
name: sync-docs
description: Playbook for updating existing generated documentation to match the current codebase, preserving manual content and removing stale sections.
---

# Skill: Sync Docs

## Purpose

Bring existing documentation under `.claude/docs/generated/` (and, if requested, `.claude/PROJECT.md`/`.claude/ARCHITECTURE.md`) up to date with the current code — additive, corrective, and stale-content-removing, never a blind rewrite.

## Inputs

- The scope to sync (a specific solution/project/folder, or explicitly "all docs" if the user really means that).
- The existing doc(s) to sync against.

## Workflow

1. **Scope**: confirm exactly which docs are being synced. Do not sync docs outside the requested scope.
2. **Read current doc**: load the existing generated doc content for the scope.
3. **Read current code**: inspect the actual current state of the scoped solution/project/folder.
4. **Diff**: identify what changed (new facts), what's now stale (no longer true), and what's unaffected.
5. **Preserve manual content**: any clearly human-authored section (e.g. a "Notes" section, an explicit manual marker) must be kept verbatim.
6. **Delegate the write**: invoke [documentation-writer](../agents/documentation-writer.md) to apply the update.
7. **Report**: summarize what was added, updated, and removed.

## Expected Outputs

- Updated doc file(s) reflecting current code, with manual content intact.
- A changelog-style summary of the sync (added/updated/removed).

## Best Practices

- Never run this as a side effect of an unrelated task — only on explicit request.
- Removing stale content is expected and desired — don't leave contradictions between docs and code.
- If it's unclear whether a section is manual or previously generated, ask rather than guessing and potentially deleting someone's manual notes.
