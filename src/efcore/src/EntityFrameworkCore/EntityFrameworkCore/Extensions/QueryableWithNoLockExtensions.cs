using Light.EntityFrameworkCore.Extensions;
using System.Linq.Expressions;
using System.Transactions;

namespace Light.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods that execute EF Core query terminal operators inside a <c>READ UNCOMMITTED</c>
/// (NOLOCK-equivalent) transaction scope.
/// </summary>
/// <remarks>
/// These extensions rely on <see cref="System.Transactions.TransactionScope"/> to set the ambient isolation level.
/// If a second physical database connection is opened and enlisted while the scope is active, <see cref="System.Transactions.TransactionScope"/>
/// will attempt to promote the transaction to a distributed transaction coordinated by MSDTC (Microsoft Distributed
/// Transaction Coordinator). MSDTC is Windows-only infrastructure and promotion will fail on Linux, which is a common
/// deployment target for net10.0 containers — avoid enlisting a second connection (e.g. a nested NOLOCK call, or any
/// other database call) inside the same scope. Additionally, <c>ReadUncommitted</c> only affects connections opened
/// *after* the scope begins: if the <see cref="DbContext"/> already has an open connection (e.g. reused from an
/// earlier operation in the same request/unit of work), the isolation level change silently does not apply to it and
/// the query runs at whatever isolation level that connection already has.
/// </remarks>
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
        var result = await queryable.ToListAsync(cancellationToken).ConfigureAwait(false);
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
        var result = await queryable.FirstAsync(cancellationToken).ConfigureAwait(false);
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
        var result = await queryable.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
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
        var result = await queryable.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
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
        var result = await queryable.SumAsync(expression, cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }

    /// <summary>
    ///     Asynchronously computes the count of a sequence of values with NO LOCK
    /// </summary>
    public static async Task<int> CountWithNoLockAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTrancation();
        var result = await queryable.CountAsync(cancellationToken).ConfigureAwait(false);
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
        var result = await queryable.ToDictionaryAsync(keySelector, elementSelector, cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return result;
    }
}
