using Light.EntityFrameworkCore.Extensions;
using System.Linq.Expressions;
using System.Transactions;

namespace Light.EntityFrameworkCore.Extensions;

public static class QueryableWithNoLockExtensions
{
    /// <summary>
    ///     Create a trancation with Read Uncommit option
    /// </summary>
    private static TransactionScope CreateTrancation()
    {
        return new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted },
            TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    ///     Asynchronously returns the list elements of a sequence with NO LOCK
    /// </summary>
    public static async Task<IEnumerable<T>> ToListWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTrancation();
        var result = await queryable.ToListAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    ///     Asynchronously returns the first element of a sequence with NO LOCK
    /// </summary>
    public static async Task<T> FirstWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTrancation();
        var result = await queryable.FirstAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    ///     Asynchronously returns the first element of a sequence,
    ///     or a default value if the sequence is empty with NO LOCK
    /// </summary>
    public static async Task<T?> FirstOrDefaultWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTrancation();
        var result = await queryable.FirstOrDefaultAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    ///     Asynchronously returns the only element of a sequence,
    ///     or a default value if the sequence is empty with NO LOCK
    /// </summary>
    public static async Task<T?> SingleOrDefaultWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTrancation();
        var result = await queryable.SingleOrDefaultAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values with NO LOCK
    /// </summary>
    public static async Task<decimal> SumWithNoLockAsync<TEntity>(this IQueryable<TEntity> queryable,
        Expression<Func<TEntity, decimal>> expression,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTrancation();
        var result = await queryable.SumAsync(expression, cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    ///     Asynchronously computes the count of a sequence of values with NO LOCK
    /// </summary>
    public static async Task<int> CountWithNoLockAsync(this IQueryable<string> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTrancation();
        var result = await queryable.CountAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    public static async Task<Dictionary<TKey, TValue>> ToDictionaryWithNoLockAsync<TSource, TKey, TValue>(
        this IQueryable<TSource> queryable,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> elementSelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        using var scope = CreateTrancation();
        var result = await queryable.ToDictionaryAsync(keySelector, elementSelector, cancellationToken);
        scope.Complete();
        return result;
    }
}