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
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}