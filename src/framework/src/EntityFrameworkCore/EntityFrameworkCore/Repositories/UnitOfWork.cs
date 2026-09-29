using Light.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
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
    /// <remarks>
    ///     A custom <see cref="IRepository{T}"/> resolved from <c>serviceProvider</c> is used as-is: this unit of
    ///     work cannot verify that it was built on the same <see cref="DbContext"/> instance. Register custom
    ///     repositories with a lifetime/scope that yields the same context (e.g. scoped, depending on the same
    ///     scoped <c>TContext</c> as this unit of work); otherwise changes made through the repository will not be
    ///     saved by <see cref="SaveChangesAsync"/> nor enlisted in this unit of work's transaction.
    /// </remarks>
    public IRepository<T> Set<T>()
        where T : class
    {
        // Wrapped in Lazy so the resolution/construction below runs at most once even under concurrent
        // first access for the same T. GetOrAdd itself may still race and create more than one Lazy
        // wrapper, but creating a Lazy has no side effects — only .Value below runs the factory, and
        // ExecutionAndPublication ensures every caller observes the single winning Lazy's result.
        var lazy = _repositories.GetOrAdd(typeof(T), _ => new Lazy<object>(() =>
        {
            // Try to resolve custom repository from application DI.
            // NOTE: the resolved repository must share this unit of work's DbContext (see remarks above).
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
    /// <remarks>
    ///     Not wrapped in an execution strategy: a retrying strategy (e.g. <c>EnableRetryOnFailure()</c>) cannot
    ///     retry a single step of a user-initiated transaction, and EF Core throws when <c>SaveChanges</c> runs
    ///     inside such a transaction. With a retrying strategy use
    ///     <see cref="ExecuteInTransactionAsync{TResult}(Func{CancellationToken, Task{TResult}}, CancellationToken)"/>
    ///     instead; the individual Begin/Commit/Rollback methods require a non-retrying strategy.
    /// </remarks>
    public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>See <see cref="BeginTransactionAsync"/> remarks regarding retrying execution strategies.</remarks>
    public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
        => await context.Database.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>See <see cref="BeginTransactionAsync"/> remarks regarding retrying execution strategies.</remarks>
    public virtual async Task RollbackAsync(CancellationToken cancellationToken = default)
        => await context.Database.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await ExecuteInTransactionAsync<object?>(async ct =>
        {
            await action(ct).ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Begin, <paramref name="action"/>, <see cref="SaveChangesAsync"/> and commit run together as one operation
    ///     of <c>Database.CreateExecutionStrategy()</c>, so a retrying strategy re-runs the whole unit on a transient
    ///     failure. <paramref name="action"/> may therefore run more than once and must be safe to re-run; note that
    ///     entities tracked by a failed attempt remain in the change tracker (call
    ///     <c>ChangeTracker.Clear()</c> at the start of the action if that matters for your workload).
    ///     If a transaction is already active on the context, the action and save simply run inside it (no new
    ///     transaction, no commit, no strategy wrapping) and the caller stays responsible for committing.
    /// </remarks>
    public virtual async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        return await ExecuteInTransactionCoreAsync(action, verifySucceeded: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public virtual async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, Func<CancellationToken, Task<bool>> verifySucceeded, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(verifySucceeded);

        await ExecuteInTransactionCoreAsync<object?>(async ct =>
        {
            await action(ct).ConfigureAwait(false);
            return null;
        }, verifySucceeded, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Same as <see cref="ExecuteInTransactionAsync{TResult}(Func{CancellationToken, Task{TResult}}, CancellationToken)"/>,
    ///     but when an attempt fails with an error the execution strategy would retry on, <paramref name="verifySucceeded"/>
    ///     is invoked first (mirrors EF Core's <c>ExecutionStrategyExtensions.ExecuteInTransactionAsync</c> with
    ///     <c>verifySucceeded</c>). If it returns <c>true</c> the failed attempt is treated as committed, no retry happens
    ///     and the result produced by the action in that attempt is returned. Use it whenever re-applying the unit would
    ///     be harmful (e.g. duplicate inserts after a commit whose acknowledgement was lost). It is ignored when joining an
    ///     ambient transaction or with a non-retrying strategy.
    /// </remarks>
    public virtual async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, Func<CancellationToken, Task<bool>> verifySucceeded, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(verifySucceeded);

        return await ExecuteInTransactionCoreAsync(action, verifySucceeded, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResult> ExecuteInTransactionCoreAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        Func<CancellationToken, Task<bool>>? verifySucceeded,
        CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            var result = await action(cancellationToken).ConfigureAwait(false);
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        var strategy = context.Database.CreateExecutionStrategy();
        var state = new TransactionState<TResult>(action, verifySucceeded);

        return await strategy.ExecuteAsync(
            state,
            async (_, s, ct) =>
            {
                // reset per attempt so verifySucceeded never returns a previous attempt's result
                s.Result = default;

                // Disposing an uncommitted transaction rolls it back, covering failures in the action, save or commit.
                await using var transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
                s.Result = await s.Action(ct).ConfigureAwait(false);
                await SaveChangesAsync(ct).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
                return s.Result;
            },
            verifySucceeded is null
                ? null
                : async (_, s, ct) => new ExecutionResult<TResult>(await s.VerifySucceeded!(ct).ConfigureAwait(false), s.Result!),
            cancellationToken).ConfigureAwait(false);
    }

    private sealed class TransactionState<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        Func<CancellationToken, Task<bool>>? verifySucceeded)
    {
        public Func<CancellationToken, Task<TResult>> Action { get; } = action;

        public Func<CancellationToken, Task<bool>>? VerifySucceeded { get; } = verifySucceeded;

        public TResult? Result { get; set; }
    }

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
