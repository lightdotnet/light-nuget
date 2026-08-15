using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Tests;

public class AppendGlobalQueryFilterExtensionTests
{
    private static DbContextOptions<TContext> NewOptions<TContext>()
        where TContext : DbContext
        => new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

    [Test]
    public void Filter_Should_Exclude_Entities_Not_Matching_Predicate()
    {
        using var context = new SoftDeleteFilterDbContext(NewOptions<SoftDeleteFilterDbContext>());

        context.Items.AddRange(
            new FilterBaseItem { Id = 1, IsDeleted = false },
            new FilterBaseItem { Id = 2, IsDeleted = true });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var result = context.Items.ToList();

        result.ShouldHaveCount(1);
        result[0].Id.ShouldBe(1);
    }

    [Test]
    public void Filter_Declared_On_Base_Type_Should_Propagate_To_Derived_Type_Queries()
    {
        using var context = new SoftDeleteFilterDbContext(NewOptions<SoftDeleteFilterDbContext>());

        // AppendGlobalQueryFilter only ever declares the filter on FilterBaseItem (where ISoftDeletable
        // is first implemented) — FilterDerivedItem must inherit it from EF Core's own TPH propagation.
        context.Set<FilterDerivedItem>().AddRange(
            new FilterDerivedItem { Id = 1, Name = "Active", IsDeleted = false },
            new FilterDerivedItem { Id = 2, Name = "Deleted", IsDeleted = true });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var result = context.Set<FilterDerivedItem>().ToList();

        result.ShouldHaveCount(1);
        result[0].Id.ShouldBe(1);
    }

    [Test]
    public void Filters_For_Different_Interfaces_On_Same_Entity_Should_Compose_With_And()
    {
        using var context = new MultiInterfaceFilterDbContext(NewOptions<MultiInterfaceFilterDbContext>());

        context.Items.AddRange(
            new FilterTenantItem { Id = 1, IsDeleted = false, TenantId = "tenant-a" }, // matches both filters
            new FilterTenantItem { Id = 2, IsDeleted = true, TenantId = "tenant-a" },  // soft-deleted
            new FilterTenantItem { Id = 3, IsDeleted = false, TenantId = "tenant-b" }); // wrong tenant
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var result = context.Items.ToList();

        result.ShouldHaveCount(1);
        result[0].Id.ShouldBe(1);
    }

    [Test]
    public void Repeated_Call_For_Same_Interface_Should_Replace_Not_And_The_Previous_Filter()
    {
        using var context = new ReplaceFilterDbContext(NewOptions<ReplaceFilterDbContext>());

        context.Items.AddRange(
            new FilterBaseItem { Id = 1, IsDeleted = false },
            new FilterBaseItem { Id = 2, IsDeleted = true });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var result = context.Items.ToList();

        // If the two calls had AND-composed, "!IsDeleted && IsDeleted" would exclude every row.
        // Because the second call replaces the first, only its predicate (IsDeleted) applies.
        result.ShouldHaveCount(1);
        result[0].Id.ShouldBe(2);
    }
}
