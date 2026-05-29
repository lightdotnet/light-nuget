# Light.Specification & Light.EntityFrameworkCore

[![NuGet](https://img.shields.io/nuget/v/Lightsoft.Specification?label=Lightsoft.Specification)](https://www.nuget.org/packages/Lightsoft.Specification)
[![NuGet](https://img.shields.io/nuget/v/Lightsoft.EntityFrameworkCore?label=Lightsoft.EntityFrameworkCore)](https://www.nuget.org/packages/Lightsoft.EntityFrameworkCore)

A lightweight **Specification Pattern** implementation with **Repository & Unit of Work** for Entity Framework Core.

## Features

- ✅ **Specification Pattern** — Type-safe, composable query filters
- ✅ **Repository & Unit of Work** — Clean data access abstraction
- ✅ **EF Core Extensions** — Specification-based queries on `DbSet<T>` and `DbContext`
- ✅ **Dapper Integration** — Raw SQL queries alongside EF Core
- ✅ **Global Query Filters** — Interface-based automatic filtering
- ✅ **NoLock Extensions** — Read Uncommitted isolation for read-heavy scenarios
- ✅ **ConfigureAwait(false)** — Library-safe async throughout
- ✅ **Thread-safe** — `ConcurrentDictionary` for repository caching

## Solution Structure

```
Solution 'EFCore' (3 projects)
├── 📁 src
│   ├── ✅ Specification (netstandard2.1)
│   │   ├── 📂 Repositories
│   │   │   ├── IQueryRepository.cs
│   │   │   ├── IRepository.cs
│   │   │   ├── ISaveChanges.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── 📂 Specification
│   │       ├── ISpecification.cs
│   │       ├── Specification.cs
│   │       ├── CollectionExtensions.cs
│   │       └── QueryableExtensions.cs
│   │
│   └── ✅ EntityFrameworkCore (.NET 10)
│       ├── 📂 EntityFrameworkCore
│       │   ├── 📂 Extensions
│       │   │   ├── AppendGlobalQueryFilterExtension.cs
│       │   │   ├── DapperExtensions.cs
│       │   │   ├── QueryableWithNoLockExtensions.cs
│       │   │   └── SpecificationExtensions.cs
│       │   ├── 📂 Repositories
│       │   │   ├── Repository.cs
│       │   │   └── UnitOfWork.cs
│       │   ├── IDbContext.cs
│       │   └── IDbSet.cs
│       └── 📂 Extensions/DependencyInjection
│           └── RepositoryServiceCollectionExtensions.cs
│
└── 🧪 Specification.Tests (NUnit)
```

## Installation

```bash
dotnet add package Lightsoft.Specification
dotnet add package Lightsoft.EntityFrameworkCore
```

## Quick Start

### 1. Define a Specification

```csharp
using Light.Specification;

public class ProductByIdSpec : Specification<Product>
{
    public ProductByIdSpec(int id)
    {
        Where(x => x.Id == id);
    }
}

// Conditional filtering with WhereIf
public class ProductFilterSpec : Specification<Product>
{
    public ProductFilterSpec(int? minId = null, string? name = null)
    {
        WhereIf(minId.HasValue, x => x.Id > minId!.Value);
        WhereIf(name != null, x => x.ProductName == name);
    }
}
```

### 2. Use with IQueryable / IEnumerable

```csharp
using Light.Specification;

var spec = new ProductByIdSpec(1);

// IQueryable (EF Core / LINQ to SQL)
var product = dbContext.Products.AsQueryable().Where(spec).First();

// IEnumerable (in-memory)
var filtered = products.Where(spec).ToList();

// Conditional WhereIf
var results = dbContext.Products.AsQueryable()
    .WhereIf(searchById, new ProductByIdSpec(id))
    .WhereIf(hasNameFilter, x => x.ProductName.Contains(name))
    .ToList();
```

### 3. Use with DbSet / DbContext Extensions

```csharp
using Light.EntityFrameworkCore.Extensions;

var spec = new ProductByIdSpec(1);

// Direct on DbSet
var product = await dbContext.Products.FirstOrDefaultAsync(spec);
var list = await dbContext.Products.ToListAsync(spec, tracking: false);
var exists = await dbContext.Products.AnyAsync(spec);
var count = await dbContext.Products.CountAsync(spec);

// Direct on DbContext
var product = await dbContext.FirstOrDefaultAsync<Product>(spec);
```

### 4. Repository & Unit of Work

```csharp
// Register in DI
services.AddUnitOfWork<AppDbContext>();

// Use in service
public class ProductService(IUnitOfWork unitOfWork)
{
    public async Task<Product?> GetByIdAsync(int id)
    {
        var repo = unitOfWork.Set<Product>();
        var spec = new ProductByIdSpec(id);
        return await repo.Where(spec).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(Product product)
    {
        var repo = unitOfWork.Set<Product>();
        await repo.AddAsync(product);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task TransactionalUpdateAsync(Product product)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            unitOfWork.Set<Product>().Update(product);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
```

### 5. Global Query Filters

```csharp
// In DbContext.OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Auto-apply soft delete filter to all entities implementing ISoftDelete
    modelBuilder.AppendGlobalQueryFilter<ISoftDelete>(e => !e.IsDeleted);

    // Conditional filter
    modelBuilder.AppendGlobalQueryFilterIf<ITenant>(
        _tenantId != null,
        e => e.TenantId == _tenantId);
}
```

### 6. Dapper Integration

```csharp
using Light.EntityFrameworkCore.Extensions;

// With Dapper parameters
var products = await dbContext.QueryAsync<Product>(
    "SELECT * FROM Products WHERE Id > @Id",
    new { Id = 5 });

// With custom reader
var products = await dbContext.QueryAsync(
    "SELECT Id, ProductName FROM Products",
    reader => new Product
    {
        Id = reader.GetInt32(0),
        ProductName = reader.GetString(1)
    });
```

### 7. NoLock Extensions

```csharp
using Light.EntityFrameworkCore.Extensions;

var products = await dbContext.Products
    .Where(x => x.IsActive)
    .ToListWithNoLockAsync();

var first = await dbContext.Products
    .Where(spec)
    .FirstOrDefaultWithNoLockAsync();

var count = await dbContext.Products
    .Where(spec)
    .CountWithNoLockAsync();
```

## API Reference

### Specification Project (`netstandard2.1`)

| Class / Interface | Description |
|---|---|
| `ISpecification<T>` | Core interface with `Expression<Func<T, bool>>` |
| `Specification<T>` | Abstract base — `Where()`, `WhereIf()`, `CompiledExpression` |
| `QueryableExtensions` | `IQueryable<T>.Where(spec)`, `.WhereIf(condition, spec)` |
| `CollectionExtensions` | `IEnumerable<T>.Where(spec)`, `.WhereIf(condition, spec)` |
| `IQueryRepository<T>` | Query interface — `Include`, `Where`, `WhereIf`, `ToListAsync`, `FindAsync` |
| `IRepository<T>` | CRUD interface — `Add`, `Update`, `Remove`, `AddAsync`, `AddRangeAsync` |
| `ISaveChanges` | `SaveChanges()`, `SaveChangesAsync()` |
| `IUnitOfWork` | `Set<T>()`, `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync` |

### EntityFrameworkCore Project (`.NET 10`)

| Class | Description |
|---|---|
| `Repository<TEntity>` | Default `IRepository<T>` implementation |
| `UnitOfWork` | Default `IUnitOfWork` with `ConcurrentDictionary` caching |
| `SpecificationExtensions` | `DbSet<T>` / `DbContext` extensions — `ToListAsync`, `SingleAsync`, `FirstAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `CountAsync` |
| `DapperExtensions` | Raw SQL via Dapper with connection lifecycle management |
| `QueryableWithNoLockExtensions` | `ReadUncommitted` isolation — `ToListWithNoLockAsync`, `FirstWithNoLockAsync`, etc. |
| `ModelBuilderExtensions` | `AppendGlobalQueryFilter<TInterface>()`, `AppendGlobalQueryFilterIf<TInterface>()` |
| `RepositoryServiceCollectionExtensions` | `AddUnitOfWork()`, `AddUnitOfWork<TContext>()` |

## Target Frameworks

| Project | Framework | C# Version |
|---|---|---|
| Specification | `netstandard2.1` | C# 8.0 |
| EntityFrameworkCore | `.NET 10` | C# 12+ |
| Tests | `.NET 10` | C# 12+ |

## License

MIT