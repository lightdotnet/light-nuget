---
name: efcore
description: Playbook for EF Core work — entity/DbContext design, migration review, and query performance — using the efcore-specialist agent.
---

# Skill: EF Core

## Purpose

Handle EF Core-specific tasks (model design, migration review, query performance) for a specific `DbContext`/project, acknowledging this repo may contain multiple independent contexts.

## Inputs

- The target `DbContext`/project (ask if not specified — never assume "the" DbContext in a multi-solution repo).
- The specific task: design a new entity, review a migration, diagnose a slow query, or review existing configuration.

## Workflow

1. **Identify the DbContext in scope**: locate it explicitly; don't assume there's only one in the repo.
2. **Delegate**: invoke [efcore-specialist](../agents/efcore-specialist.md) with the specific task and scope.
3. **For migrations**: review the specific migration file(s) for destructive operations before considering them safe to apply.
4. **For performance**: get the actual query/LINQ shape from the user or the code, not a hypothetical.
5. **Report**: findings/design ranked by risk (data loss/breaking > performance > style), with concrete fixes.

## Expected Outputs

- A reviewed/designed entity, DbContext configuration, or migration.
- For performance tasks: a specific diagnosis and fix tied to the actual query.

## Best Practices

- Never apply migrations to a real database without explicit confirmation.
- Treat shared base entities (if any) as higher-risk to change — they may affect multiple consumer contexts.
- Don't recommend schema changes without noting migration/consumer impact.
