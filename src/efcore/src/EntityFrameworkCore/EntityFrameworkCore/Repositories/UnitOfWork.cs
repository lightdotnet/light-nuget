using Light.Repositories;
using System.Collections.Concurrent;

namespace Light.EntityFrameworkCore.Repositories;

/// <inheritdoc/>
public class UnitOfWork(DbContext context, IServiceProvider? serviceProvider = null) : IUnitOfWork
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
        => await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
        => await context.Database.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual async Task RollbackAsync(CancellationToken cancellationToken = default)
        => await context.Database.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);

    public void Dispose()
    {
        context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}

/// <inheritdoc/>
public class UnitOfWork<TContext>(TContext context, IServiceProvider? serviceProvider = null)
    : UnitOfWork(context, serviceProvider), IUnitOfWork<TContext>
    where TContext : DbContext;