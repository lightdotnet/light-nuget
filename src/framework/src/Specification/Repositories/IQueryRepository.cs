using Light.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Repositories
{
    public interface IQueryRepository<T> where T : class
    {
        IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath);
        IQueryable<T> Where(Expression<Func<T, bool>> expression);
        IQueryable<T> WhereIf(bool condition, Expression<Func<T, bool>> expression);

        /// <summary>
        /// Applies only the filter of <paramref name="specification"/>. Ordering and paging carried by an
        /// <see cref="IOrderedSpecification{T}"/> are ignored; use <see cref="Apply(ISpecification{T})"/> for those.
        /// </summary>
        IQueryable<T> Where(ISpecification<T> specification);

        IQueryable<T> WhereIf(bool condition, ISpecification<T> specification);

        /// <summary>
        /// Applies the filter, ordering and paging of <paramref name="specification"/>
        /// (same pipeline as <see cref="QueryableExtensions.Apply{T}(IQueryable{T}, ISpecification{T})"/>).
        /// </summary>
        IQueryable<T> Apply(ISpecification<T> specification)
            => Where(specification).ApplyOrderingAndPaging(specification);

        Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);
        Task<T?> FindAsync<TKey>(TKey key, CancellationToken cancellationToken = default) where TKey : notnull;
        Task<T?> FindAsync(object?[] key, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    }
}
