# TODO — Light.EntityFrameworkCore

Open follow-ups from an EF Core–focused review of this project, scoped to `src/EntityFrameworkCore` (`Framework.slnx`).
All previously tracked correctness bugs and design gaps have been fixed — see
[README.md](README.md) (Registration, NOLOCK Query Extensions, Global Query Filter sections, plus the
**Known Limitations** section) for the resulting behavior and caveats. See also
[Specification's README](../Specification/README.md) for the Combinators section covering the interfaces this
project implements.

---

## Resolved

- [x] **`AppendGlobalQueryFilter<TInterface>` had no test coverage.**
  `EntityFrameworkCore/Extensions/AppendGlobalQueryFilterExtension.cs`
  Fixed — added `AppendGlobalQueryFilterExtensionTests.cs` in `EntityFrameworkCore.Tests`, backed by three
  dedicated `ModelBuilder`/`OnModelCreating` fixture `DbContext`s (`GlobalQueryFilterTestDbContexts.cs`, one per
  scenario rather than a single flag-driven context, since EF Core caches a context's model per context *type*).
  Covers: predicate filtering, TPH base-to-derived propagation, AND-composition of different interfaces on the
  same entity, and same-interface repeat-call replace semantics.

- [x] **`UnitOfWork.Set<T>()` factory could run more than once under concurrent first access.**
  `EntityFrameworkCore/Repositories/UnitOfWork.cs`
  Fixed — `_repositories` is now `ConcurrentDictionary<Type, Lazy<object>>` with
  `LazyThreadSafetyMode.ExecutionAndPublication`, so the resolve/construct delegate runs at most once per `T`
  regardless of how many threads call `Set<T>()` concurrently on first access.
  **This was more severe than originally assessed** — the "harmless for the default path" note above was wrong.
  Reproducing the bug (see `UnitOfWorkConcurrencyTests.cs`, using a `Transient`-registered custom repository with
  an artificial constructor delay to widen the race) didn't just double-construct: concurrent first calls to
  `Set<T>()` crashed the process with `InvalidOperationException: Operations that change non-concurrent
  collections must have exclusive access`, because `Repository<T>`'s constructor calls `context.Set<TEntity>()`,
  which mutates a plain, non-thread-safe `Dictionary` inside `DbContext`'s internal `IDbSetCache`. That means the
  *default* `Repository<T>` path was exposed too, not just custom DI-registered repositories — any first
  concurrent access to the same `UnitOfWork` for a given entity type could crash. See README's Known Limitations
  section for the corrected description.

  **Residual gap, intentionally left as a documented usage constraint rather than more code:** the `Lazy` fix
  only serializes concurrent first access for the *same* `T`. Two threads racing on *different* `T`s (e.g.
  `Set<Product>()` vs `Set<Order>()`) on the same `UnitOfWork` still hit the underlying `DbContext`
  unsynchronized and can still corrupt it — `DbContext` itself isn't thread-safe, which isn't something
  `UnitOfWork` can fix without a much heavier lock (hurting the common single-threaded-per-scope case for a
  problem that's really a misuse pattern). Documented instead, in the `UnitOfWork` XML doc `<remarks>` and
  README's Known Limitations/Registration sections: never share one `UnitOfWork`/`DbContext` instance across
  concurrently running threads — give each one its own scope.
