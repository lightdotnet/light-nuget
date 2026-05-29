using System.Linq.Expressions;
using System.Transactions;

namespace Light.EntityFrameworkCore.Extensions;

public static class QueryableWithNoLockExtensions
{
    /// <summary>
    /// Create a transaction with Read Uncommit option
    /// </summary>
    private static TransactionScope CreateTransaction()
    {
        return new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted },
            TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    /// Asynchronously returns the list elements of a sequence with NO LOCK
    /// </summary>
    public static async Task<IEnumerable<T>> ToListWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTransaction();
        var result = await queryable.ToListAsync(cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Asynchronously returns the first element of a sequence with NO LOCK
    /// </summary>
    public static async Task<T> FirstWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTransaction();
        var result = await queryable.FirstAsync(cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Asynchronously returns the first element of a sequence,
    /// or a default value if the sequence is empty with NO LOCK
    /// </summary>
    public static async Task<T?> FirstOrDefaultWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTransaction();
        var result = await queryable.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Asynchronously returns the only element of a sequence,
    /// or a default value if the sequence is empty with NO LOCK
    /// </summary>
    public static async Task<T?> SingleOrDefaultWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTransaction();
        var result = await queryable.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values with NO LOCK
    /// </summary>
    public static async Task<decimal> SumWithNoLockAsync<TEntity>(this IQueryable<TEntity> queryable,
        Expression<Func<TEntity, decimal>> expression,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTransaction();
        var result = await queryable.SumAsync(expression, cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Asynchronously computes the count of a sequence of values with NO LOCK
    /// </summary>
    public static async Task<int> CountWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTransaction();
        var result = await queryable.CountAsync(cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Asynchronously converts a sequence to a dictionary with NO LOCK
    /// </summary>
    public static async Task<Dictionary<TKey, TValue>> ToDictionaryWithNoLockAsync<TSource, TKey, TValue>(
        this IQueryable<TSource> queryable,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> elementSelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        using var scope = CreateTransaction();
        var result = await queryable.ToDictionaryAsync(keySelector, elementSelector, cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }
}