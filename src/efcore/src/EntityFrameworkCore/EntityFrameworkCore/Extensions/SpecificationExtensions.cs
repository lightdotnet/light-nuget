using Light.Specification;

namespace Light.EntityFrameworkCore.Extensions;

public static class SpecificationExtensions
{
    #region [Private Helpers]

    /// <summary>
    /// Apply specification filter, ordering, paging &amp; tracking behaviour
    /// </summary>
    private static IQueryable<T> Apply<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
    {
        var query = dbSet.AsQueryable().Apply(specification);
        if (tracking is false) query = query.AsNoTracking();
        return query;
    }

    /// <summary>
    /// Apply specification filter &amp; tracking behaviour (no ordering/paging — for aggregates)
    /// </summary>
    private static IQueryable<T> Where<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
    {
        var query = dbSet.AsQueryable().Where(specification);
        if (tracking is false) query = query.AsNoTracking();
        return query;
    }

    #endregion

    #region [DbSet — Data Methods (use Apply)]

    /// <summary>
    /// Get list of T by specification
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, true).ToListAsync(cancellationToken);

    /// <summary>
    /// Get list of T by specification with tracking option
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, tracking).ToListAsync(cancellationToken);

    /// <summary>
    /// Get single T by specification
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, true).SingleAsync(cancellationToken);

    /// <summary>
    /// Get single T by specification with tracking option
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, tracking).SingleAsync(cancellationToken);

    /// <summary>
    /// Get single T or default by specification
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, true).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Get single T or default by specification with tracking option
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, tracking).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Get first T by specification
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, true).FirstAsync(cancellationToken);

    /// <summary>
    /// Get first T by specification with tracking option
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, tracking).FirstAsync(cancellationToken);

    /// <summary>
    /// Get first T or default by specification
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, true).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Get first T or default by specification with tracking option
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Apply(specification, tracking).FirstOrDefaultAsync(cancellationToken);

    #endregion

    #region [DbSet — Aggregate Methods (use Where — no ordering/paging)]

    /// <summary>
    /// Check if any T exists by specification
    /// </summary>
    public static Task<bool> AnyAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, false).AnyAsync(cancellationToken);

    /// <summary>
    /// Count T by specification
    /// </summary>
    public static Task<int> CountAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, false).CountAsync(cancellationToken);

    #endregion

    #region [DbContext — Data Methods (delegate to DbSet)]

    /// <summary>
    /// Build queryable with specification and tracking
    /// </summary>
    public static IQueryable<T> Where<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
        => context.Set<T>().Where(specification, tracking);

    /// <summary>
    /// Apply specification with ordering and paging
    /// </summary>
    public static IQueryable<T> Apply<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
        => context.Set<T>().Apply(specification, tracking);

    /// <summary>
    /// Get list of T by specification
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().ToListAsync(specification, cancellationToken);

    /// <summary>
    /// Get list of T by specification with tracking option
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().ToListAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get single T by specification
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleAsync(specification, cancellationToken);

    /// <summary>
    /// Get single T by specification with tracking option
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get single T or default by specification
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleOrDefaultAsync(specification, cancellationToken);

    /// <summary>
    /// Get single T or default by specification with tracking option
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleOrDefaultAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get first T by specification
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstAsync(specification, cancellationToken);

    /// <summary>
    /// Get first T by specification with tracking option
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get first T or default by specification
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstOrDefaultAsync(specification, cancellationToken);

    /// <summary>
    /// Get first T or default by specification with tracking option
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstOrDefaultAsync(specification, tracking, cancellationToken);

    #endregion

    #region [DbContext — Aggregate Methods (delegate to DbSet)]

    /// <summary>
    /// Check if any T exists by specification
    /// </summary>
    public static Task<bool> AnyAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().AnyAsync(specification, cancellationToken);

    /// <summary>
    /// Count T by specification
    /// </summary>
    public static Task<int> CountAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().CountAsync(specification, cancellationToken);

    #endregion
}