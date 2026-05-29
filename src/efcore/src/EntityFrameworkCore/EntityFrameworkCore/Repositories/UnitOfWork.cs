using Light.Repositories;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Concurrent;

namespace Light.EntityFrameworkCore.Repositories;

/// <inheritdoc/>
public class UnitOfWork(DbContext context) : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    /// <inheritdoc/>
    public IRepository<T> Set<T>()
        where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ =>
        {
            // Try to resolve custom repository from DI first
            var customRepository = context.GetService<IRepository<T>>();
            if (customRepository is not null)
                return customRepository;

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
public class UnitOfWork<TContext>(TContext context) : UnitOfWork(context), IUnitOfWork<TContext>
    where TContext : DbContext;