using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Repositories
{
    /// <summary>
    /// Can be used to query, add, update, remove instances of T.
    /// SaveChanges should be handled by IUnitOfWork.
    /// </summary>
    public interface IRepository<T> : IQueryRepository<T> where T : class
    {
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    }
}