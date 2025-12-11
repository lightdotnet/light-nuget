using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Repositories
{
    /// <summary>
    ///     Use to query and save instances of T with Repository patterns
    /// </summary>
    public interface IUnitOfWork : ISaveChanges, IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     Can be used to query, add, update, remove instances of T
        /// </summary>
        IRepository<T> Set<T>(bool useCustomRepository = false) where T : class;

        /// <summary>
        ///     Asynchronously begin a new transaction.
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Asynchronously commit transaction.
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Asynchronously rollback transaction.
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}