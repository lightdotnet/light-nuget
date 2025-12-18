using Light.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Repositories
{
    /// <summary>
    ///     Use to query instances of T.
    /// </summary>
    public interface IQueryRepository<T> where T : class
    {
        /// <summary>
        ///     Query instances of T with Include.
        /// </summary>
        IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath);

        /// <summary>
        ///     Query instances of T by expression.
        /// </summary>
        IQueryable<T> Where(Expression<Func<T, bool>> expression);

        /// <summary>
        ///     Query instances of T by expression if condition is true.
        /// </summary>
        IQueryable<T> WhereIf(bool condition, Expression<Func<T, bool>> expression);

        /// <summary>
        ///     Query instances of T by specification.
        /// </summary>
        IQueryable<T> Where(ISpecification<T> specification);

        /// <summary>
        ///     Query instances of T by specification if condition is true.
        /// </summary>
        IQueryable<T> WhereIf(bool condition, ISpecification<T> specification);

        /// <summary>
        ///     Asynchronous query all instances of T.
        /// </summary>
        Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Asynchronous find a instance of T by key.
        /// </summary>
        Task<T?> FindAsync<TKey>(TKey key, CancellationToken cancellationToken = default) where TKey : notnull;

        /// <summary>
        ///     Asynchronous find a instance of T by object ids.
        /// </summary>
        Task<T?> FindAsync(object?[] key, CancellationToken cancellationToken = default);
    }
}