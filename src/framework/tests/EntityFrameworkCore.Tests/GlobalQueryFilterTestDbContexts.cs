using Light.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Tests;

// Each scenario gets its own DbContext type rather than flags on a shared context: EF Core caches
// a context's model per context type, so a single reusable type with instance-level configuration
// would silently reuse the first-built model across differently-configured instances.

public class SoftDeleteFilterDbContext(DbContextOptions<SoftDeleteFilterDbContext> options) : DbContext(options)
{
    public DbSet<FilterBaseItem> Items => Set<FilterBaseItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FilterDerivedItem>();

        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(x => !x.IsDeleted);
    }
}

public class MultiInterfaceFilterDbContext(DbContextOptions<MultiInterfaceFilterDbContext> options) : DbContext(options)
{
    public DbSet<FilterTenantItem> Items => Set<FilterTenantItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(x => !x.IsDeleted);
        modelBuilder.AppendGlobalQueryFilter<ITenantScoped>(x => x.TenantId == "tenant-a");
    }
}

public class ReplaceFilterDbContext(DbContextOptions<ReplaceFilterDbContext> options) : DbContext(options)
{
    public DbSet<FilterBaseItem> Items => Set<FilterBaseItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(x => !x.IsDeleted);
        // Second call for the SAME interface — should replace the first filter, not AND with it.
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(x => x.IsDeleted);
    }
}
