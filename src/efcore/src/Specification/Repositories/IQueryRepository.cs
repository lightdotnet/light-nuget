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
        IQueryable<T> Where(ISpecification<T> specification);
        IQueryable<T> WhereIf(bool condition, ISpecification<T> specification);
        Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);
        Task<T?> FindAsync<TKey>(TKey key, CancellationToken cancellationToken = default) where TKey : notnull;
        Task<T?> FindAsync(object?[] key, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    }
}