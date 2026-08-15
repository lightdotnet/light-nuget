---
name: generate-docs
description: Playbook for generating new documentation from code for a specific scope, using the templates in .claude/docs/templates/.
---

# Skill: Generate Docs

## Purpose

Produce new documentation for a specific solution/project/folder that doesn't yet have generated docs, using the standard templates — never repo-wide, never speculative.

## Inputs

- The target scope (solution/project/folder) to document.
- Which doc type is wanted (overview, architecture, API, database, domain model, conventions, dependency graph, dev guide) — see [docs/templates/](../docs/templates/).

## Workflow

1. **Scope and doc type**: confirm exactly what's being documented and which template applies.
2. **Read the template**: load the matching file from `.claude/docs/templates/` for structure.
3. **Delegate**: invoke [documentation-writer](../agents/documentation-writer.md) with the scope and template.
4. **Verify facts**: every claim in the generated doc must trace to actual code inspected for this scope — mark anything unverifiable as `unknown` rather than guessing.
5. **Write output**: place the result under `.claude/docs/generated/<scope>/<doc-type>.md`.
6. **Report**: summarize what was generated and flag any gaps found during generation.

## Expected Outputs

- A new markdown file under `.claude/docs/generated/` following the template structure, populated only with verified facts.
- A short summary of what was generated and any open questions.

## Best Practices

- Don't generate docs for scopes not requested, even if adjacent.
- Don't carry over assumptions from similar solutions — verify per-scope.
- If a doc already exists for this scope, use [sync-docs](sync-docs.md) instead of blindly overwriting it.
