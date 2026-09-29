using Light.EntityFrameworkCore.Repositories;
using Light.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.Tests;

/// <summary>
/// Transaction tests against in-memory Sqlite (InMemory ignores transactions). The context is configured with a
/// retrying execution strategy, mirroring providers configured with <c>EnableRetryOnFailure()</c>.
/// </summary>
public class UnitOfWorkTransactionTests
{
    private SqliteConnection _connection = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var context = CreateContext(retrying: false);
        context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown() => _connection.Dispose();

    private TestDbContext CreateContext(bool retrying)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection, o =>
            {
                if (retrying) o.ExecutionStrategy(d => new TestRetryingExecutionStrategy(d));
            })
            .Options;
        return new TestDbContext(options);
    }

    private int CountProducts()
    {
        using var context = CreateContext(retrying: false);
        return context.Products.Count();
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Commit_With_Retrying_Strategy()
    {
        await using var uow = new UnitOfWork(CreateContext(retrying: true));

        await uow.ExecuteInTransactionAsync(async ct =>
            await uow.Set<Product>().AddAsync(new Product { Id = 1, ProductName = "Committed" }, ct));

        CountProducts().ShouldBe(1);
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Retry_Whole_Unit_On_Transient_Failure()
    {
        await using var uow = new UnitOfWork(CreateContext(retrying: true));
        var attempts = 0;

        var result = await uow.ExecuteInTransactionAsync(async ct =>
        {
            attempts++;
            if (attempts == 1) throw new TransientTestException();
            await uow.Set<Product>().AddAsync(new Product { Id = 1, ProductName = "Retried" }, ct);
            return attempts;
        });

        result.ShouldBe(2);
        CountProducts().ShouldBe(1);
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Rollback_On_Failure()
    {
        await using var uow = new UnitOfWork(CreateContext(retrying: true));

        Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync(async ct =>
        {
            uow.Set<Product>().Add(new Product { Id = 1, ProductName = "Rolled back" });
            await uow.SaveChangesAsync(ct); // written inside the transaction...
            throw new InvalidOperationException("boom"); // ...then discarded by rollback
        }));

        CountProducts().ShouldBe(0);
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Join_Ambient_Transaction()
    {
        await using var uow = new UnitOfWork(CreateContext(retrying: false));

        await uow.BeginTransactionAsync();
        await uow.ExecuteInTransactionAsync(ct =>
        {
            uow.Set<Product>().Add(new Product { Id = 1, ProductName = "Ambient" });
            return Task.CompletedTask;
        });
        await uow.RollbackAsync();

        CountProducts().ShouldBe(0);
    }

    [Test]
    public async Task Individual_Transaction_Methods_Should_Commit_And_Rollback_With_Default_Strategy()
    {
        await using var uow = new UnitOfWork(CreateContext(retrying: false));

        await uow.BeginTransactionAsync();
        uow.Set<Product>().Add(new Product { Id = 1, ProductName = "Rolled back" });
        await uow.SaveChangesAsync();
        await uow.RollbackAsync();
        CountProducts().ShouldBe(0);

        uow.Set<Product>().Add(new Product { Id = 2, ProductName = "Committed" });
        await uow.BeginTransactionAsync();
        await uow.SaveChangesAsync();
        await uow.CommitAsync();
        CountProducts().ShouldBe(1);
    }

    [Test]
    public async Task Default_Interface_ExecuteInTransactionAsync_Should_Commit()
    {
        // Exercises the default interface implementation used by IUnitOfWork implementers that do not override it.
        await using var inner = new UnitOfWork(CreateContext(retrying: false));
        IUnitOfWork uow = new MinimalUnitOfWork(inner);

        await uow.ExecuteInTransactionAsync(ct =>
        {
            uow.Set<Product>().Add(new Product { Id = 1, ProductName = "Default impl" });
            return Task.CompletedTask;
        });

        CountProducts().ShouldBe(1);
    }

    private sealed class TransientTestException : Exception;

    private sealed class TestRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, maxRetryCount: 3, maxRetryDelay: TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TransientTestException;

        protected override TimeSpan? GetNextDelay(Exception lastException)
            => base.GetNextDelay(lastException) is null ? null : TimeSpan.Zero;
    }

    /// <summary>Implements only the abstract IUnitOfWork members, delegating to a real UnitOfWork.</summary>
    private sealed class MinimalUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public IRepository<T> Set<T>() where T : class => inner.Set<T>();
        public int SaveChanges() => inner.SaveChanges();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => inner.SaveChangesAsync(cancellationToken);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => inner.BeginTransactionAsync(cancellationToken);
        public Task CommitAsync(CancellationToken cancellationToken = default) => inner.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken = default) => inner.RollbackAsync(cancellationToken);
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
