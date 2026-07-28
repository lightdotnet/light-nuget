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
}
