using Light.EntityFrameworkCore.Repositories;
using Light.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Extensions.DependencyInjection;

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Add UnitOfWork with default Repository
    /// </summary>
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
        return services;
    }

    /// <summary>
    /// Add UnitOfWork with specific DbContext
    /// </summary>
    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork>(sp =>
            new UnitOfWork<TContext>(sp.GetRequiredService<TContext>(), sp));
        services.AddScoped<IUnitOfWork<TContext>>(sp =>
            new UnitOfWork<TContext>(sp.GetRequiredService<TContext>(), sp));
        return services;
    }

    /// <summary>
    /// Add UnitOfWork with custom implementation
    /// </summary>
    public static IServiceCollection AddUnitOfWork<TInterface, TImplement>(this IServiceCollection services)
        where TInterface : class, IUnitOfWork
        where TImplement : class, TInterface
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<TInterface, TImplement>();
        return services;
    }
}