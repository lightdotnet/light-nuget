# Light.Specification & Light.EntityFrameworkCore

[![NuGet](https://img.shields.io/nuget/v/Lightsoft.Specification?label=Light.Specification)](https://www.nuget.org/packages/Lightsoft.Specification)
[![NuGet](https://img.shields.io/nuget/v/Lightsoft.EntityFrameworkCore?label=Light.EntityFrameworkCore)](https://www.nuget.org/packages/Lightsoft.EntityFrameworkCore)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A lightweight **Specification Pattern** library with **Repository + Unit of Work** for .NET — minimal, composable, and EF Core–friendly.

---

## ✨ Features

| Feature | Package |
|---------|---------|
| `ISpecification<T>` — expression-based specification interface | `Light.Specification` |
| `Specification<T>` — base class with `Where`, `WhereIf`, `OrderBy`, `Paging` | `Light.Specification` |
| `IsSatisfiedBy(entity)` — in-memory evaluation | `Light.Specification` |
| `And` / `Or` / `Not` combinators | `Light.Specification` |
| `IQueryable<T>.Apply(spec)` — filter + order + page pipeline | `Light.Specification` |
| `IEnumerable<T>.Where(spec)` — in-memory filtering with cached compiled expression | `Light.Specification` |
| `IRepository<T>` / `IUnitOfWork` — repository & unit of work interfaces | `Light.Specification` |
| `Repository<T>` / `UnitOfWork` — EF Core implementations | `Light.EntityFrameworkCore` |
| `DbSet<T>.ToListAsync(spec)`, `SingleAsync`, `FirstAsync`, etc. | `Light.EntityFrameworkCore` |
| `DbSet<T>.AnyAsync(spec)`, `CountAsync(spec)` | `Light.EntityFrameworkCore` |
| `NOLOCK` query extensions | `Light.EntityFrameworkCore` |
| Dapper integration via `DbContext` | `Light.EntityFrameworkCore` |
| Append global query filters dynamically | `Light.EntityFrameworkCore` |

---

## 📦 Installation

```bash
# Specification + Repository interfaces (netstandard2.0)
dotnet add package Lightsoft.Specification

# EF Core implementations (.NET 10)
dotnet add package Lightsoft.EntityFrameworkCore
```

---

## 🚀 Quick Start

### 1. Define a Specification

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

### 2. Use with EF Core

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

// Apply: filter → order → page in one call
var result = dbContext.Set<Product>()
    .AsQueryable()
    .Apply(spec)
    .ToList();
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

> **Note:** Combinators have **no `where T : class` constraint** — they work with any `T`.

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
var query = dbContext.Products.Where(spec);
var query = dbContext.Products.WhereIf(condition, spec);

// Full pipeline: filter + ordering + paging
var query = dbContext.Products.Apply(spec);
```

---

## 🗂️ Repository & Unit of Work

### Interfaces

```
IQueryRepository<T>
  ├── Include, Where, WhereIf
  ├── ToListAsync, FindAsync
  └── CountAsync, AnyAsync          ← NEW

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

### Registration

```csharp
// Basic — auto-creates Repository<T> for any entity
services.AddUnitOfWork<AppDbContext>();

// Custom UnitOfWork implementation
services.AddUnitOfWork<IAppUnitOfWork, AppUnitOfWork>();
```

### Custom Repository via DI

```csharp
// Register a custom repository
services.AddScoped<IRepository<Product>, ProductRepository>();

// UnitOfWork.Set<Product>() will resolve ProductRepository from DI
var repo = uow.Set<Product>(); // → ProductRepository instance
```

### Usage

```csharp
public class OrderService(IUnitOfWork uow)
{
    public async Task CreateOrder(Order order)
    {
        await uow.BeginTransactionAsync();

        uow.Set<Order>().Add(order);
        uow.Set<OrderItem>().AddRange(order.Items);

        await uow.SaveChangesAsync();
        await uow.CommitAsync();
    }
}
```

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

```csharp
var result = await dbContext.Products
    .WithNoLock()
    .Where(x => x.IsActive)
    .ToListWithNoLockAsync();
```

### Dapper Extensions

```csharp
var products = await dbContext.QueryAsync<Product>(
    "SELECT * FROM Products WHERE Price > @Price",
    new { Price = 100m });
```

### Global Query Filter

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Append soft-delete filter to all entities implementing ISoftDelete
    modelBuilder.AppendGlobalQueryFilter<ISoftDelete>(x => !x.IsDeleted);
}
```

---

## 📋 API Reference

### Light.Specification

| Type | Members |
|------|---------|
| `ISpecification<T>` | `Expression` |
| `Specification<T>` | `Where`, `WhereIf`, `OrderBy`, `OrderByDescending`, `ApplyPaging`, `IsSatisfiedBy`, `CompiledExpression`, `OrderByExpressions`, `Skip`, `Take` |
| `OrderByExpression<T>` | `KeySelector`, `IsDescending` |
| `SpecificationCombinators` | `And<T>`, `Or<T>`, `Not<T>` |
| `CollectionExtensions` | `Where<T>(ISpecification)`, `WhereIf<T>` |
| `QueryableExtensions` | `Where<T>(ISpecification)`, `WhereIf<T>`, `Apply<T>` |

### Light.Repositories (interfaces)

| Type | Members |
|------|---------|
| `ISaveChanges` | `SaveChanges`, `SaveChangesAsync` |
| `IQueryRepository<T>` | `Include`, `Where`, `WhereIf`, `ToListAsync`, `FindAsync`, `CountAsync`, `AnyAsync` |
| `IRepository<T>` | Inherits `IQueryRepository<T>` + `Add`, `AddRange`, `Update`, `UpdateRange`, `Remove`, `RemoveRange`, `AddAsync`, `AddRangeAsync` |
| `IUnitOfWork` | Inherits `ISaveChanges` + `Set<T>`, `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync` |

### Light.EntityFrameworkCore

| Type | Members |
|------|---------|
| `Repository<TEntity>` | Implements `IRepository<T>` with `ConfigureAwait(false)` |
| `UnitOfWork` | Implements `IUnitOfWork`, resolves custom repos via `IServiceProvider` |
| `SpecificationExtensions` | `ToListAsync`, `SingleAsync`, `SingleOrDefaultAsync`, `FirstAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync` — on both `DbSet<T>` and `DbContext` |
| `DapperExtensions` | `QueryAsync<T>` on `DbContext` |
| `QueryableWithNoLockExtensions` | `WithNoLock`, `ToListWithNoLockAsync`, `CountWithNoLockAsync`, etc. |
| `AppendGlobalQueryFilterExtension` | `AppendGlobalQueryFilter<TInterface>` on `ModelBuilder` |
| `RepositoryServiceCollectionExtensions` | `AddUnitOfWork`, `AddUnitOfWork<TContext>`, `AddUnitOfWork<TInterface, TImplement>` |

---

## 🧪 Tests

| Project | Tests |
|---------|-------|
| `Specification.Tests` | 38 |
| `EntityFrameworkCore.Tests` — RepositoryTests | 22 |
| `EntityFrameworkCore.Tests` — SpecificationExtensionsTests | 21 |
| `EntityFrameworkCore.Tests` — UnitOfWorkTests | 6 |
| **Total** | **87** |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).