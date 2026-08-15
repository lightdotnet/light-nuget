# Generated Documentation

This directory holds documentation generated from actual code, produced only by explicit [generate-docs](../../skills/generate-docs.md) / [sync-docs](../../skills/sync-docs.md) requests (see [workflows/sync-documentation.md](../../workflows/sync-documentation.md)). Nothing here is created automatically as a side effect of other work.

## Layout Convention

Docs are organized by scope, mirroring the repo's multi-solution shape:

```text
docs/generated/
├── <SolutionName>/
│   ├── overview.md              # from templates/solution-overview.md
│   ├── architecture.md          # from templates/architecture.md
│   ├── database.md              # from templates/database.md
│   ├── domain-model.md          # from templates/domain-model.md
│   ├── api.md                   # from templates/api-documentation.md
│   ├── dependency-graph.md      # from templates/dependency-graph.md
│   ├── coding-conventions.md    # from templates/coding-conventions.md
│   ├── development-guide.md     # from templates/development-guide.md
│   └── <ProjectName>/
│       └── overview.md          # from templates/project-overview.md
└── repository-overview.md       # from templates/repository-overview.md, whole-repo scope only
```

Each generated file carries a `<!-- manual -->`-marked section (see the templates) that must be preserved verbatim during any sync.

This directory is currently empty — nothing has been generated yet. It is populated incrementally, one scope at a time, on request.
