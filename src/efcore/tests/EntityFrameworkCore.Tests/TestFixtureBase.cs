using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Tests;

/// <summary>
/// Base class for EF Core tests using InMemory database.
/// </summary>
public abstract class TestFixtureBase
{
    protected TestDbContext Context { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new TestDbContext(options);

        SeedData();
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    private void SeedData()
    {
        Context.Products.AddRange(
            new Product { Id = 1, ProductName = "Product 1", Price = 10.0m, IsActive = true },
            new Product { Id = 2, ProductName = "Product 2", Price = 20.0m, IsActive = true },
            new Product { Id = 3, ProductName = "Product 3", Price = 30.0m, IsActive = false },
            new Product { Id = 4, ProductName = "Product 4", Price = 40.0m, IsActive = true },
            new Product { Id = 5, ProductName = "Product 5", Price = 50.0m, IsActive = false }
        );
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }
}