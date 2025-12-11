using Light.EntityFrameworkCore.Repositories;
using Light.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Extensions.DependencyInjection;

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Use default Repositories & Unit Of Work
    ///     Entities & DbContext will inject manually
    /// </summary>
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));

        return services;
    }

    /// <summary>
    /// Only use Unit Of Work, repository will auto inject with default
    /// </summary>
    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();

        return services;
    }

    /// <summary>
    /// Use custom Unit Of Work implement from base UOW abstractions
    /// </summary>
    public static IServiceCollection AddUnitOfWork<TInterface, TImplement>(this IServiceCollection services)
        where TInterface : IUnitOfWork
        where TImplement : class, TInterface
    {
        services.AddScoped(typeof(TInterface), typeof(TImplement));

        return services;
    }
}