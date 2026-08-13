[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.Specification

Root namespace: `Light` · Package/assembly: `Lightsoft.Specification` · Target: `netstandard2.1`

A lightweight, expression-based **Specification Pattern** for .NET, plus the context-agnostic
**Repository + Unit of Work** interfaces built on top of it — minimal, composable, and usable
with any `IQueryable`/`IEnumerable` source. No EF Core dependency; see
[Lightsoft.EntityFrameworkCore](../EntityFrameworkCore/README.md) for the EF Core implementations
of the repository/unit-of-work interfaces defined here.

Leaf project — no `ProjectReference`s. Within this solution, `EntityFrameworkCore` is the only
project that references `Specification`.

---

## ✨ Contents

| Type | Namespace | Purpose |
|---|---|---|
| `ISpecification<T>` | `Light.Specification` | Expression-based specification contract — `Expression` |
| `Specification<T>` | `Light.Specification` | Base class with `Where`, `WhereIf`, `OrderBy`, `OrderByDescending`, `ApplyPaging`, `IsSatisfiedBy`, `CompiledExpression`, `OrderByExpressions`, `Skip`, `Take` |
| `OrderByExpression<T>` | `Light.Specification` | `KeySelector`, `IsDescending` |
| `SpecificationCombinators` | `Light.Specification` | `And<T>`, `Or<T>`, `Not<T>` |
| `CollectionExtensions` | `Light.Specification` | `Where<T>(ISpecification)`, `WhereIf<T>` — for `IEnumerable<T>` |
| `QueryableExtensions` | `Light.Specification` | `Where<T>(ISpecification)`, `WhereIf<T>`, `Apply<T>` — for `IQueryable<T>` |
| `ISaveChanges` | `Light.Repositories` | `SaveChanges`, `SaveChangesAsync` |
| `IQueryRepository<T>` | `Light.Repositories` | `Include`, `Where`, `WhereIf`, `ToListAsync`, `FindAsync`, `CountAsync`, `AnyAsync` |
| `IRepository<T>` | `Light.Repositories` | Inherits `IQueryRepository<T>` + `Add`, `AddRange`, `Update`, `UpdateRange`, `Remove`, `RemoveRange`, `AddAsync`, `AddRangeAsync` |
| `IUnitOfWork` | `Light.Repositories` | Inherits `ISaveChanges` + `Set<T>`, `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync` |

---

## 🚀 Quick Start

```csharp
using Light.Specification;

public class ActiveProductSpec : Specification<Product>
{
    public ActiveProductSpec()
    {
        Where(x => x.IsActive);
    }
}

public class ProductByPriceRangeSpec : Specification<Product>
{
    public ProductByPriceRangeSpec(decimal min, decimal max)
    {
        Where(x => x.Price >= min);
        Where(x => x.Price <= max);       // AND combined automatically
        OrderBy(x => (object)x.Price);     // ascending
        ApplyPaging(skip: 0, take: 20);    // pagination
    }
}
```

---

## 📐 Specification Pattern

### Basic Where / WhereIf

```csharp
public class ProductSearchSpec : Specification<Product>
{
    public ProductSearchSpec(string? name = null, bool? isActive = null)
    {
        WhereIf(!string.IsNullOrEmpty(name), x => x.Name.Contains(name!));
        WhereIf(isActive.HasValue, x => x.IsActive == isActive!.Value);
    }
}
```

### IsSatisfiedBy — In-Memory Evaluation

```csharp
var spec = new ActiveProductSpec();

if (spec.IsSatisfiedBy(product))
{
    Console.WriteLine("Product is active");
}
```

### OrderBy + Paging

```csharp
public class LatestProductsSpec : Specification<Product>
{
    public LatestProductsSpec(int page, int pageSize)
    {
        Where(x => x.IsActive);
        OrderByDescending(x => x.CreatedAt);
        ApplyPaging(skip: (page - 1) * pageSize, take: pageSize);
    }
}

// Apply: filter → order → page in one call, against any IQueryable<T>
var result = products.AsQueryable().Apply(spec).ToList();
```

### Combinators — And / Or / Not

```csharp
ISpecification<Product> active = new ActiveProductSpec();
ISpecification<Product> premium = new PremiumProductSpec();

// Combine dynamically
var activeAndPremium = active.And(premium);
var activeOrPremium  = active.Or(premium);
var inactive         = active.Not();

// Use anywhere
var results = products.AsQueryable().Where(activeAndPremium).ToList();
```

> **Note:** Combinators require `T : class` (matching `Specification<T>`) and preserve ordering/paging from
> whichever operand carries it (left-biased for `And`/`Or`; `Not` preserves its source spec's ordering/paging).
> The underlying `ISpecification<T>` interface itself has no such constraint, so custom specifications for
> value types can still be authored outside the combinator/`Apply` pipeline.

### Collection Filtering (IEnumerable)

```csharp
var spec = new ActiveProductSpec();

// Uses cached CompiledExpression for performance
var filtered = products.Where(spec).ToList();
var maybe    = products.WhereIf(applyFilter, spec).ToList();
```

### Queryable Extensions (IQueryable)

```csharp
// Filter only (no ordering/paging)
var query = products.AsQueryable().Where(spec);
var query = products.AsQueryable().WhereIf(condition, spec);

// Full pipeline: filter + ordering + paging
var query = products.AsQueryable().Apply(spec);
```

---

## 🗂️ Repository & Unit of Work interfaces

These are context-agnostic contracts only — this package ships no implementation. Use
[Lightsoft.EntityFrameworkCore](../EntityFrameworkCore/README.md) for EF Core–backed
`Repository<T>` / `UnitOfWork` implementations, or implement them yourself against any
persistence technology.

```
IQueryRepository<T>
  ├── Include, Where, WhereIf
  ├── ToListAsync, FindAsync
  └── CountAsync, AnyAsync

IRepository<T> : IQueryRepository<T>
  ├── Add, AddRange, AddAsync, AddRangeAsync
  ├── Update, UpdateRange
  └── Remove, RemoveRange

IUnitOfWork : ISaveChanges, IDisposable, IAsyncDisposable
  ├── Set<T>()                       → resolves custom or default repo
  ├── BeginTransactionAsync
  ├── CommitAsync
  └── RollbackAsync
```

---

## 🧪 Tests

`Specification.Tests` — 42 tests covering `Specification<T>`, combinators, and the
collection/queryable extension methods.
