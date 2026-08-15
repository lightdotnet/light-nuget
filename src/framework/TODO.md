# TODO — Framework

Open follow-ups for the `Framework` solution (`Framework.slnx`). See each project's own `README.md` for
current, user-facing documentation.

## Open items

- [ ] **`.github/workflows/publish-identity-to-nuget.yml` still references the deleted Identity projects.**
  References `src/Identity/Identity.csproj` and `src/Identity.EntityFrameworkCore/Identity.EntityFrameworkCore.csproj`,
  both deleted. It's `workflow_dispatch`-only (not triggered automatically), so it doesn't break CI, but it would
  fail if manually run. Left untouched — modifying CI/CD workflows needs explicit sign-off separately.
