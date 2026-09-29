using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Repositories
{
    /// <summary>
    /// Use to query and save instances of T with Repository patterns
    /// </summary>
    public interface IUnitOfWork : ISaveChanges, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Get or create a repository for T.
        /// If a custom IRepository&lt;T&gt; is registered in DI, it will be used;
        /// otherwise a default repository is created.
        /// </summary>
        IRepository<T> Set<T>() where T : class;

        /// <summary>
        /// Begins a user-initiated transaction.
        /// </summary>
        /// <remarks>
        /// Providers configured with a retrying execution strategy (e.g. EF Core <c>EnableRetryOnFailure()</c>)
        /// do not support user-initiated transactions driven by separate Begin/Commit/Rollback calls. Prefer
        /// <see cref="ExecuteInTransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>, which runs the
        /// whole unit of work (begin, work, save, commit) as a single retriable operation.
        /// </remarks>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the transaction started by <see cref="BeginTransactionAsync"/>.
        /// See <see cref="BeginTransactionAsync"/> remarks regarding retrying execution strategies.
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction started by <see cref="BeginTransactionAsync"/>.
        /// See <see cref="BeginTransactionAsync"/> remarks regarding retrying execution strategies.
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs <paramref name="action"/>, saves changes and commits, all inside one transaction.
        /// The transaction is rolled back if <paramref name="action"/>, the save or the commit throws.
        /// </summary>
        /// <remarks>
        /// Implementations backed by a retrying execution strategy (e.g. the EF Core <c>UnitOfWork</c>) may invoke
        /// <paramref name="action"/> more than once, so it must be safe to re-run: load and modify data inside the
        /// action rather than relying on state captured before the call.
        /// This default implementation (used by implementers that do not override it) simply composes
        /// <see cref="BeginTransactionAsync"/>, <see cref="ISaveChanges.SaveChangesAsync"/>,
        /// <see cref="CommitAsync"/> and <see cref="RollbackAsync"/> without any retry support.
        /// </remarks>
        async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await ExecuteInTransactionAsync<object?>(async ct =>
            {
                await action(ct).ConfigureAwait(false);
                return null;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs <paramref name="action"/>, saves changes and commits, all inside one transaction, returning the
        /// result of the action. The transaction is rolled back if the action, the save or the commit throws.
        /// </summary>
        /// <remarks>
        /// See <see cref="ExecuteInTransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>.
        /// </remarks>
        async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await action(cancellationToken).ConfigureAwait(false);
                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
    }
}
