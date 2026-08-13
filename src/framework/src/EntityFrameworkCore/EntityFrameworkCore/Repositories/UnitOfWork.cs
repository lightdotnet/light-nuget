using Light.Repositories;
using System.Collections.Concurrent;

namespace Light.EntityFrameworkCore.Repositories;

/// <inheritdoc/>
/// <remarks>
///     Not safe to use from multiple threads concurrently. <see cref="Set{T}"/> serializes concurrent first
///     access for the same <c>T</c>, but different <c>T</c>s racing each other still reach the underlying
///     <see cref="DbContext"/> unsynchronized, which can corrupt its internal state — <see cref="DbContext"/>
///     itself is not thread-safe. Give each concurrently running unit of work its own scoped instance instead
///     of sharing one <see cref="UnitOfWork"/> across threads.
/// </remarks>
/// <param name="context">The DbContext used by this unit of work.</param>
/// <param name="serviceProvider">Optional service provider used to resolve custom repositories.</param>
/// <param name="ownsContext">
///     Whether this instance owns <paramref name="context"/> and should dispose it.
///     Set to <c>false</c> when the context's lifetime is managed elsewhere (e.g. by the DI container).
/// </param>
public class UnitOfWork(DbContext context, IServiceProvider? serviceProvider = null, bool ownsContext = true) : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _repositories = new();

    /// <inheritdoc/>
    public IRepository<T> Set<T>()
        where T : class
    {
        // Wrapped in Lazy so the resolution/construction below runs at most once even under concurrent
        // first access for the same T. GetOrAdd itself may still race and create more than one Lazy
        // wrapper, but creating a Lazy has no side effects — only .Value below runs the factory, and
        // ExecutionAndPublication ensures every caller observes the single winning Lazy's result.
        var lazy = _repositories.GetOrAdd(typeof(T), _ => new Lazy<object>(() =>
        {
            // Try to resolve custom repository from application DI
            if (serviceProvider?.GetService(typeof(IRepository<T>)) is IRepository<T> custom)
                return custom;

            // Fallback to default repository
            return new Repository<T>(context);
        }, LazyThreadSafetyMode.ExecutionAndPublication));

        return (IRepository<T>)lazy.Value;
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
