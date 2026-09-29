using Light.Extensions.DependencyInjection;
using Light.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.Tests;

public class UnitOfWorkDependencyInjectionTests
{
    [Test]
    public void AddUnitOfWork_Early_Dispose_Should_Not_Dispose_Shared_Context()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o =>
            o.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        services.AddUnitOfWork();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<TestDbContext>>();
        uow.Dispose();

        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        Assert.DoesNotThrow(() => context.Products.Add(new Product { Id = 999, ProductName = "Still usable" }));
    }

    [Test]
    public void AddUnitOfWork_TContext_Should_Resolve_Same_Instance_For_Generic_And_NonGeneric()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o =>
            o.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        services.AddUnitOfWork<TestDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var generic = scope.ServiceProvider.GetRequiredService<IUnitOfWork<TestDbContext>>();
        var nonGeneric = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Assert.That(nonGeneric, Is.SameAs(generic));
    }
}
