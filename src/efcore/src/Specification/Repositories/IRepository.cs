using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Repositories
{
    /// <summary>
    ///     Can be used to query, add, update, remove instances of T
    /// </summary>
    public interface IRepository<T> : IQueryRepository<T>, ISaveChanges where T : class
    {
        /// <summary>
        ///     Add a instance of T.
        /// </summary>
        void Add(T entity);

        /// <summary>
        ///     Add list instances of T.
        /// </summary>
        void AddRange(IEnumerable<T> entities);

        /// <summary>
        ///     Update a instance of T.
        /// </summary>
        void Update(T entity);

        /// <summary>
        ///     Update list instances of T.
        /// </summary>
        void UpdateRange(IEnumerable<T> entities);

        /// <summary>
        ///     Remove a instance of T.
        /// </summary>
        void Remove(T entity);

        /// <summary>
        ///     Remove list instances of T.
        /// </summary>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        ///     Asynchronously add a instance of T.
        /// </summary>
        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Asynchronously add list instances of T.
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    }
}