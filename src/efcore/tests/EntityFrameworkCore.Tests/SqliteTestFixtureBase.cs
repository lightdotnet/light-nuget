using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Tests;

/// <summary>
/// Base class for EF Core tests that need real SQL translation (e.g. boxed value-type expressions),
/// backed by an in-memory Sqlite database.
/// </summary>
public abstract class SqliteTestFixtureBase
{
    private SqliteConnection _connection = null!;
    protected TestDbContext Context { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        // Sqlite in-memory databases live only as long as the connection that created them stays open —
        // keep this connection open for the duration of the test, not just DbContext construction.
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new TestDbContext(options);
        Context.Database.EnsureCreated();

        SeedData();
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
        _connection.Dispose();
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
