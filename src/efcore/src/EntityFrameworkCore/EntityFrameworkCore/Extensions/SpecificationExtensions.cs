using Light.Specification;

namespace Light.EntityFrameworkCore.Extensions;

public static class SpecificationExtensions
{
    #region [DbSet]

    /// <summary>
    /// Build a queryable filter by specification &amp; tracking behaviour
    /// </summary>
    private static IQueryable<T> Where<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
    {
        var query = dbSet.AsQueryable().Where(specification);

        if (tracking is false)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    /// <summary>
    /// Get list instances of T by specification
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).ToListAsync(cancellationToken);

    /// <summary>
    /// Get list instances of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).ToListAsync(cancellationToken);

    /// <summary>
    /// Get single instance of T by specification
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).SingleAsync(cancellationToken);

    /// <summary>
    /// Get single instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).SingleAsync(cancellationToken);

    /// <summary>
    /// Get single instance of T or default by specification
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Get single instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Get first instance of T by specification
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).FirstAsync(cancellationToken);

    /// <summary>
    /// Get first instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).FirstAsync(cancellationToken);

    /// <summary>
    /// Get first instance of T or default by specification
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Get first instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Check if any instance of T exists by specification
    /// </summary>
    public static Task<bool> AnyAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, false).AnyAsync(cancellationToken);

    /// <summary>
    /// Count instances of T by specification
    /// </summary>
    public static Task<int> CountAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, false).CountAsync(cancellationToken);

    #endregion

    #region [DbContext]

    /// <summary>
    /// Build a queryable filter by specification &amp; tracking behaviour
    /// </summary>
    public static IQueryable<T> Where<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
        => context.Set<T>().Where(specification, tracking);

    /// <summary>
    /// Get list instances of T by specification
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().ToListAsync(specification, cancellationToken);

    /// <summary>
    /// Get list instances of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().ToListAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get single instance of T by specification
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleAsync(specification, cancellationToken);

    /// <summary>
    /// Get single instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get single instance of T or default by specification
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleOrDefaultAsync(specification, cancellationToken);

    /// <summary>
    /// Get single instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().SingleOrDefaultAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get first instance of T by specification
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstAsync(specification, cancellationToken);

    /// <summary>
    /// Get first instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Get first instance of T or default by specification
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstOrDefaultAsync(specification, cancellationToken);

    /// <summary>
    /// Get first instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().FirstOrDefaultAsync(specification, tracking, cancellationToken);

    /// <summary>
    /// Check if any instance of T exists by specification
    /// </summary>
    public static Task<bool> AnyAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().AnyAsync(specification, cancellationToken);

    /// <summary>
    /// Count instances of T by specification
    /// </summary>
    public static Task<int> CountAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Set<T>().CountAsync(specification, cancellationToken);

    #endregion
}