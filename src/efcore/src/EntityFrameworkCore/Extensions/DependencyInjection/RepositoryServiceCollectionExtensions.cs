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
        services.AddScoped(typeof(IUnitOfWork<>), typeof(ScopedUnitOfWork<>));
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
            new UnitOfWork<TContext>(sp.GetRequiredService<TContext>(), sp, ownsContext: false));
        services.AddScoped<IUnitOfWork<TContext>>(sp =>
            new UnitOfWork<TContext>(sp.GetRequiredService<TContext>(), sp, ownsContext: false));
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

    /// <summary>
    /// DI-only <see cref="UnitOfWork{TContext}"/> variant used by the open-generic
    /// <see cref="IServiceCollection.AddScoped(System.Type, System.Type)"/> registration in
    /// <see cref="AddUnitOfWork(IServiceCollection)"/>. Always resolves a container-owned, scoped
    /// <typeparamref name="TContext"/> and therefore must never dispose it itself — the container
    /// disposes it when the scope ends. Exists solely because open-generic type-mapping registrations
    /// have no way to pin a non-service constructor argument (here, <c>ownsContext: false</c>)
    /// per registration.
    /// </summary>
    private sealed class ScopedUnitOfWork<TContext>(TContext context, IServiceProvider? serviceProvider = null)
        : UnitOfWork<TContext>(context, serviceProvider, ownsContext: false)
        where TContext : DbContext;
}
