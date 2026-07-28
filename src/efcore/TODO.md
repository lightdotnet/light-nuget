# TODO — Light.Specification / Light.EntityFrameworkCore

Open follow-ups from an EF Core–focused review of this solution (`EFCore.slnx`), scoped to `src/Specification` and
`src/EntityFrameworkCore`. All previously tracked correctness bugs and design gaps have been fixed — see
[README.md](README.md) (Registration, NOLOCK Query Extensions, Global Query Filter, and Combinators sections, plus
the **Known Limitations** section) for the resulting behavior and caveats.

---

## Open items

- [ ] **`AppendGlobalQueryFilter<TInterface>` has no test coverage.**
  `src/EntityFrameworkCore/EntityFrameworkCore/Extensions/AppendGlobalQueryFilterExtension.cs`
  No test project currently exercises this extension. Would need new `ModelBuilder`/`OnModelCreating` fixture
  infrastructure not present in the test projects today (existing fixtures use a pre-built `TestDbContext`, not a
  bare `ModelBuilder`).

- [ ] **`UnitOfWork.Set<T>()` factory can run more than once under concurrent first access.**
  `src/EntityFrameworkCore/EntityFrameworkCore/Repositories/UnitOfWork.cs:12-24`
  `ConcurrentDictionary.GetOrAdd` doesn't guarantee single execution of the factory. Harmless for the default
  `new Repository<T>(context)` path, but could double-construct a custom DI-registered repository with side effects
  in its constructor. Reviewed and intentionally left as-is — revisit if that scenario becomes real. See README's
  Known Limitations section.
