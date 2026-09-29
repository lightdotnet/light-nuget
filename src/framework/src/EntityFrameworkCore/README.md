[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.EntityFrameworkCore

Root namespace: `Light` · Package/assembly: `Lightsoft.EntityFrameworkCore` · Target: `net10.0`

EF Core implementations of the [Lightsoft.Specification](../Specification/README.md) package's
`IRepository<T>` / `IUnitOfWork` interfaces, plus a set of EF Core–specific extensions: applying
specifications directly to `DbSet<T>`/`DbContext`, `NOLOCK` query helpers, Dapper integration, and
dynamic global query filters.

- **Dependencies:** `Dapper`, `Microsoft.EntityFrameworkCore.Relational`
- **`ProjectReference`:** [`Specification`](../Specification/Specification.csproj) — this package builds on
  `Specification`'s `ISpecification<T>` and repository/unit-of-work interfaces.

---

## ✨ Contents

| Type | Namespace | Purpose |
|---|---|---|
| `Repository<TEntity>` | `Light.EntityFrameworkCore.Repositories` | Implements `IRepository<T>` with `ConfigureAwait(false)` |
| `Repository<TEntity, TContext>` | `Light.EntityFrameworkCore.Repositories` | `Repository<TEntity>` pinned to a specific `TContext : DbContext`, implements `IRepository<TEntity, TContext>` |
| `UnitOfWork` | `Light.EntityFrameworkCore.Repositories` | Implements `IUnitOfWork`, resolves custom repos via `IServiceProvider`, caches resolved repositories per entity type |
| `UnitOfWork<TContext>` | `Light.EntityFrameworkCore.Repositories` | `UnitOfWork` pinned to a specific `TContext : DbContext`, implements `IUnitOfWork<TContext>` |
| `IRepository<TEntity, TContext>` | `Light.Repositories` | `IRepository<TEntity>` constrained to a specific `TContext : DbContext` |
| `IUnitOfWork<TContext>` | `Light.Repositories` | `IUnitOfWork` constrained to a specific `TContext : DbContext` |
| `IDbContext` | `Light.EntityFrameworkCore` | Minimal `DbContext` abstraction — `ChangeTracker`, `Database`, `Entry<T>`, `SaveChangesAsync` |
| `IDbSet` | `Light.EntityFrameworkCore` | `Set<TEntity>()` abstraction for dynamic `DbSet<T>` resolution |
| `SpecificationExtensions` | `Light.EntityFrameworkCore` | `ToListAsync`, `SingleAsync`, `SingleOrDefaultAsync`, `FirstAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync` — on both `DbSet<T>` and `DbContext` |
| `DapperExtensions` | `Light.EntityFrameworkCore` | Two `QueryAsync<T>` overloads on `DbContext`: one delegates to Dapper's `IDbConnection.QueryAsync<T>` (SQL + object param); the other takes a manual `Func<DbDataReader, T> map` delegate and reads via raw ADO.NET (`DbCommand`/`DbDataReader`), without going through Dapper's own mapping. |
| `QueryableWithNoLockExtensions` | `Light.EntityFrameworkCore` | Terminal-operator extensions on `IQueryable<T>`, each opening its own `TransactionScope(ReadUncommitted)` around the query: `ToListWithNoLockAsync`, `FirstWithNoLockAsync`, `FirstOrDefaultWithNoLockAsync`, `SingleOrDefaultWithNoLockAsync`, `SumWithNoLockAsync`, `CountWithNoLockAsync`, `ToDictionaryWithNoLockAsync`. There is no separate `WithNoLock()` call — each method is a full terminal operator you call directly on the query. |
| `ModelBuilderExtensions` | `Light.EntityFrameworkCore` | `AppendGlobalQueryFilter<TInterface>` / `AppendGlobalQueryFilterIf<TInterface>(condition, filter)` on `ModelBuilder`. (File is named `AppendGlobalQueryFilterExtension.cs`, but the class itself is `ModelBuilderExtensions`.) |
| `RepositoryServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddUnitOfWork`, `AddUnitOfWork<TContext>`, `AddUnitOfWork<TInterface, TImplement>` |

---

## 🚀 Quick Start

```csharp
// Register in DI
services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
services.AddUnitOfWork<AppDbContext>();

// Use in service
public class ProductService(IUnitOfWork uow)
{
    public async Task<IReadOnlyList<Product>> GetActiveProducts()
    {
        var repo = uow.Set<Product>();
        var spec = new ActiveProductSpec();
        return await repo.Where(spec).ToListAsync();
    }
}
```

See [Lightsoft.Specification](../Specification/README.md) for how to define `ISpecification<T>`
implementations like `ActiveProductSpec` above.

---

## 🗂️ Repository & Unit of Work

### Registration

```csharp
// Basic — auto-creates Repository<T> for any entity
services.AddUnitOfWork<AppDbContext>();

// Custom UnitOfWork implementation
services.AddUnitOfWork<IAppUnitOfWork, AppUnitOfWork>();
```

> **Note:** `UnitOfWork` never disposes a `DbContext` it doesn't own when resolved through `AddUnitOfWork()`/`AddUnitOfWork<TContext>()` — those registrations resolve a scoped, container-owned context, so disposing the `IUnitOfWork` early is safe and won't break other scoped services sharing that context. If you construct `UnitOfWork` directly (`new UnitOfWork(context)`), it owns and disposes the context by default; pass `ownsContext: false` to opt out.
>
> **Transactions and retrying execution strategies:** `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` call the `DbContext` directly and are **not** retriable. They require a non-retrying execution strategy — with a retry-enabled provider (e.g. `EnableRetryOnFailure()`) EF Core rejects `SaveChanges` inside a user-initiated transaction. Use `ExecuteInTransactionAsync(...)` instead: it runs begin → your action → `SaveChangesAsync` → commit as **one** `CreateExecutionStrategy().ExecuteAsync(...)` operation, rolling back on failure and re-running the whole unit on a transient error (so the action must be safe to re-run). If a transaction is already active on the context, `ExecuteInTransactionAsync` joins it (runs the action + save, no commit, no strategy wrapping). Entities tracked by a failed attempt stay in the change tracker — call `ChangeTracker.Clear()` at the start of the action if that matters.
>
> **Overloads:** `ExecuteInTransactionAsync(action)` and `ExecuteInTransactionAsync<TResult>(action)` (returns the action's result), plus the same two with a `Func<CancellationToken, Task<bool>> verifySucceeded` parameter. When an attempt fails with an error the strategy would retry, `verifySucceeded` is invoked first (mirrors EF Core's `ExecuteInTransactionAsync(..., verifySucceeded)`); returning `true` treats the attempt as committed — no retry, and the result from that attempt is returned. Use it when re-applying the unit would be harmful (e.g. duplicate inserts after a commit whose acknowledgement was lost); it should do a fresh store read (e.g. check for a unique marker written by the action), not consult the change tracker. It is ignored when joining an ambient transaction or with a non-retrying strategy. Inheritors: the `verifySucceeded` overloads do not delegate to the other overloads — override both sets when customizing.
>
> `AddUnitOfWork<TContext>()` resolves `IUnitOfWork` and `IUnitOfWork<TContext>` to the **same** scoped instance. The non-generic `IUnitOfWork` is registered with `TryAdd`, so with several contexts it maps to the **first** registered one — inject `IUnitOfWork<TContext>` to target a specific context.
>
> **Never share a single `UnitOfWork`/`DbContext` instance across concurrent threads** (e.g. fan-out with `Task.WhenAll` over the same injected `IUnitOfWork`). This is a standing EF Core constraint, not something `UnitOfWork` can fully guard against: `Set<T>()` serializes concurrent *first* access for the *same* `T`, but concurrent first access for *different* entity types (e.g. one thread calling `Set<Product>()` while another calls `Set<Order>()`) still reaches the underlying `DbContext` unsynchronized and can corrupt its internal state. Give each concurrent unit of work its own scope/`DbContext` instead.

### Custom Repository via DI

```csharp
// Register a custom repository
services.AddScoped<IRepository<Product>, ProductRepository>();

// UnitOfWork.Set<Product>() will resolve ProductRepository from DI
var repo = uow.Set<Product>(); // → ProductRepository instance
```

> **Note:** `UnitOfWork` cannot verify that a custom repository resolved from DI uses the *same* `DbContext` instance as the unit of work. Register it as scoped and inject the same scoped `TContext`; otherwise its changes are not saved by `uow.SaveChangesAsync()` nor enlisted in the unit of work's transaction.

### Usage

```csharp
public class OrderService(IUnitOfWork uow)
{
    public async Task CreateOrder(Order order)
    {
        // begin + action + SaveChangesAsync + commit, retriable as one unit
        await uow.ExecuteInTransactionAsync(ct =>
        {
            uow.Set<Order>().Add(order);
            uow.Set<OrderItem>().AddRange(order.Items);
            return Task.CompletedTask;
        });
    }
}
```

With a non-retrying execution strategy you can still drive the transaction manually:

```csharp
await uow.BeginTransactionAsync();
uow.Set<Order>().Add(order);
await uow.SaveChangesAsync();
await uow.CommitAsync();
```

Repositories also expose `Apply(spec)` / `Apply(spec, tracking)` (filter + ordering + paging); `Where(spec)` applies the filter only.

---

## ⚡ EF Core Extensions

### SpecificationExtensions — DbSet / DbContext

```csharp
var spec = new ActiveProductSpec();

// DbSet extensions
var list   = await dbContext.Set<Product>().ToListAsync(spec);
var list   = await dbContext.Set<Product>().ToListAsync(spec, tracking: false);
var single = await dbContext.Set<Product>().SingleAsync(spec);
var first  = await dbContext.Set<Product>().FirstOrDefaultAsync(spec);
var exists = await dbContext.Set<Product>().AnyAsync(spec);
var count  = await dbContext.Set<Product>().CountAsync(spec);

// DbContext shorthand
var list   = await dbContext.ToListAsync(spec);
var count  = await dbContext.CountAsync(spec);
```

> **Data methods** (`ToList`, `Single`, `First`) use `Apply()` — filter + order + page.
> **Aggregate methods** (`Any`, `Count`) use `Where()` — filter only (no order/page).

### NOLOCK Query Extensions

Each extension is a self-contained terminal operator — build the `IQueryable<T>` normally, then call the
`*WithNoLockAsync` method last instead of the regular `ToListAsync`/`CountAsync`/etc.:

```csharp
var result = await dbContext.Products
    .Where(x => x.IsActive)
    .ToListWithNoLockAsync();

var count = await dbContext.Products.CountWithNoLockAsync();
```

> **Caveats:** these extensions use `TransactionScope(IsolationLevel.ReadUncommitted)` under the hood.
> `ReadUncommitted` only affects connections opened *after* the scope begins — if the `DbContext`'s connection is
> already open, the isolation level silently does not apply. If a second physical connection gets enlisted while
> the scope is active, `TransactionScope` will attempt to promote to a distributed transaction coordinated by
> MSDTC, which is Windows-only and will fail on Linux (a common `net10.0` container target) — avoid nesting NOLOCK
> calls or other database calls inside the same scope.

### Dapper Extensions

```csharp
var products = await dbContext.QueryAsync<Product>(
    "SELECT * FROM Products WHERE Price > @Price",
    new { Price = 100m });
```

Use the `QueryAsync<T>(query, param, commandType, cancellationToken)` overload to pass a `CancellationToken`. All overloads enlist in the context's current transaction (`Database.CurrentTransaction`) when one is active.

A second overload, `QueryAsync<T>(this DbContext, string query, Func<DbDataReader, T> map, CancellationToken)`,
skips Dapper's own mapping and lets you map each row yourself from a raw `DbDataReader` — useful when Dapper's
convention-based mapping doesn't fit. It opens/closes the underlying connection around the read itself, rather
than delegating to Dapper's connection handling.

### Global Query Filter

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Append soft-delete filter to all entities implementing ISoftDelete
    modelBuilder.AppendGlobalQueryFilter<ISoftDelete>(x => !x.IsDeleted);
}
```

> **Note:** the filter is registered under a stable, per-interface key (`$"Global_{typeof(TInterface).FullName}"`),
> so filters for *different* interfaces on the same entity compose automatically via AND (EF Core 10 combines all
> named/default filters natively). Calling `AppendGlobalQueryFilter<TInterface>` more than once for the *same*
> interface **replaces** the previously registered filter for that key rather than AND-ing the two calls together.
> The filter is applied once per entity hierarchy, at the point where `TInterface` is first implemented — EF Core
> propagates it to derived types automatically. `AppendGlobalQueryFilterIf<TInterface>(condition, filter)` is a
> conditional wrapper that only calls `AppendGlobalQueryFilter` when `condition` is `true`.
>
> Call it **at the end** of `OnModelCreating` (after `ApplyConfigurationsFromAssembly` etc.) — it only sees entity types already in the model. Owned types are skipped (EF Core does not allow filters on them; they are filtered via their owner).

> **No audit / soft-delete interceptor is included.** `AppendGlobalQueryFilter<ISoftDelete>` only *hides* soft-deleted rows; `Repository.Remove`/`RemoveRange` on an `ISoftDelete` entity performs a **hard delete**, and audit fields are not populated automatically. Consumers must add their own `SaveChangesInterceptor` (or `SaveChanges` override) to convert deletes into `IsDeleted = true` updates and to stamp audit data.

---

## 🧪 Tests

| Suite | Tests |
|---|---|
| `RepositoryTests` | 22 |
| `SpecificationExtensionsTests` | 21 |
| `UnitOfWorkTests` | 8 |
| `UnitOfWorkTransactionTests` | 8 |
| `AppendGlobalQueryFilterExtensionTests` | 4 |
| `QueryableWithNoLockExtensionsTests` | 2 |
| `UnitOfWorkDependencyInjectionTests` | 2 |
| `UnitOfWorkConcurrencyTests` | 1 |
| `SpecificationSqliteTests` | 1 |
| **Total** | **69** |

`EntityFrameworkCore.Tests` runs against the EF Core InMemory provider by default; `SpecificationSqliteTests` uses
an in-memory Sqlite database instead, specifically to verify real SQL translation for boxed value-type `OrderBy`
key selectors (e.g. `x => (object)x.Price`), which InMemory skips entirely.

---

## ⚠️ Known Limitations

- See the caveats inline above for **NOLOCK extensions** (MSDTC/Linux, stale open connections) and
  **`AppendGlobalQueryFilter`** (same-interface repeat-call replaces rather than ANDs).
- `UnitOfWork.Set<T>()` caches repositories in a `ConcurrentDictionary<Type, Lazy<object>>`; the resolve/construct
  step for a given `T` runs at most once even under concurrent first access from multiple threads (see
  [TODO.md](TODO.md) for why this needed fixing — the earlier plain `ConcurrentDictionary<Type, object>` could
  crash the process under concurrent first access, not just double-construct).
- **This only covers same-`T` access.** A single `UnitOfWork`/`DbContext` instance is still not safe to use from
  multiple threads concurrently — e.g. one thread's first call to `Set<Product>()` racing another thread's first
  call to `Set<Order>()` on the *same* `UnitOfWork` reaches the underlying `DbContext` unsynchronized and can still
  corrupt its internal state (`DbContext` itself is not thread-safe; this is an EF Core-wide constraint, not
  specific to `UnitOfWork`). Use a separate `DbContext`/`UnitOfWork` (i.e. a separate DI scope) per concurrently
  running unit of work — never fan out (`Task.WhenAll`, parallel threads, etc.) over one shared instance.
