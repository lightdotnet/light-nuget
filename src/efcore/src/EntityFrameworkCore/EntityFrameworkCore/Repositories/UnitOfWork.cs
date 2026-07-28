using Light.Repositories;
using System.Collections.Concurrent;

namespace Light.EntityFrameworkCore.Repositories;

/// <inheritdoc/>
/// <param name="context">The DbContext used by this unit of work.</param>
/// <param name="serviceProvider">Optional service provider used to resolve custom repositories.</param>
/// <param name="ownsContext">
///     Whether this instance owns <paramref name="context"/> and should dispose it.
///     Set to <c>false</c> when the context's lifetime is managed elsewhere (e.g. by the DI container).
/// </param>
public class UnitOfWork(DbContext context, IServiceProvider? serviceProvider = null, bool ownsContext = true) : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    /// <inheritdoc/>
    public IRepository<T> Set<T>()
        where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ =>
        {
            // Try to resolve custom repository from application DI
            if (serviceProvider?.GetService(typeof(IRepository<T>)) is IRepository<T> custom)
                return custom;

            // Fallback to default repository
            return new Repository<T>(context);
        });
    }

    /// <inheritdoc/>
    public virtual int SaveChanges() => context.SaveChanges();

    /// <inheritdoc/>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await context.Database.CreateExecutionStrategy()
            .ExecuteAsync(cancellationToken, ct => context.Database.BeginTransactionAsync(ct))
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
        => await context.Database.CreateExecutionStrategy()
            .ExecuteAsync(cancellationToken, ct => context.Database.CommitTransactionAsync(ct))
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual async Task RollbackAsync(CancellationToken cancellationToken = default)
        => await context.Database.CreateExecutionStrategy()
            .ExecuteAsync(cancellationToken, ct => context.Database.RollbackTransactionAsync(ct))
            .ConfigureAwait(false);

    public void Dispose()
    {
        if (ownsContext) context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (ownsContext) await context.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}

/// <inheritdoc/>
public class UnitOfWork<TContext>(TContext context, IServiceProvider? serviceProvider = null, bool ownsContext = true)
    : UnitOfWork(context, serviceProvider, ownsContext), IUnitOfWork<TContext>
    where TContext : DbContext;