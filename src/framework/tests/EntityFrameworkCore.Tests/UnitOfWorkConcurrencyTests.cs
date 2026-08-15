using Light.EntityFrameworkCore.Repositories;
using Light.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.Tests;

public class UnitOfWorkConcurrencyTests
{
    [SetUp]
    public void ResetCounter() => CountingRepository<Product>.ConstructedCount = 0;

    [Test]
    public void Set_Should_Construct_Custom_Repository_Exactly_Once_Under_Concurrent_First_Access()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        // Force the model to build up front, outside the race window below — otherwise the model
        // build itself (not the bug under test) could be the source of any observed race.
        context.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton<DbContext>(context);
        // Transient is what actually exposes the bug: a Scoped registration would be protected by
        // the DI container's own per-scope locking regardless of what UnitOfWork.Set<T>() does.
        services.AddTransient<IRepository<Product>, CountingRepository<Product>>();
        using var provider = services.BuildServiceProvider();

        using var uow = new UnitOfWork(context, provider);

        // Real OS threads (not Task.Run) so all of them are actually running concurrently when they
        // hit Set<Product>() for the first time — a thread-pool queue could otherwise serialize them
        // and hide the race.
        var results = new IRepository<Product>?[16];
        var threads = new Thread[results.Length];
        for (var i = 0; i < threads.Length; i++)
        {
            var index = i;
            threads[i] = new Thread(() => results[index] = uow.Set<Product>());
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        CountingRepository<Product>.ConstructedCount.ShouldBe(1);
        results.Distinct().ShouldHaveCount(1);
    }
}
