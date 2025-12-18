using Light.EntityFrameworkCore.Extensions;
using Light.Specification;

namespace Light.EntityFrameworkCore.Extensions;

public static class SpecificationExtensions
{
    #region[DbSet]

    /// <summary>
    ///     build a queryable filter by specification & tracking behaviour
    /// </summary>
    private static IQueryable<T> Where<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
    {
        // filter by specification
        var query = dbSet.AsQueryable().Where(specification);

        // check Tracking
        if (tracking is false)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    /// <summary>
    ///     Get list instances of T by specification
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).ToListAsync(cancellationToken);

    /// <summary>
    ///     Get list instances of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).ToListAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T by specification
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).SingleAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).SingleAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T or default by specification
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T by specification
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).FirstAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).FirstAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T or default by specification
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, true).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbSet<T> dbSet,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => dbSet.Where(specification, tracking).FirstOrDefaultAsync(cancellationToken);

    #endregion

    #region[DbContext]

    /// <summary>
    ///     build a queryable filter by specification & tracking behaviour
    /// </summary>
    public static IQueryable<T> Where<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking = true)
        where T : class
        => context.Set<T>().Where(specification, tracking);

    /// <summary>
    ///     Get list instances of T by specification
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, true).ToListAsync(cancellationToken);

    /// <summary>
    ///     Get list instances of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, tracking).ToListAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T by specification
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, true).SingleAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> SingleAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, tracking).SingleAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T or default by specification
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, true).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    ///     Get single instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> SingleOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, tracking).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T by specification
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, true).FirstAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T by specification with tracking or no-tracking
    /// </summary>
    public static Task<T> FirstAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, tracking).FirstAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T or default by specification
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, true).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    ///     Get first instance of T or default by specification with tracking or no-tracking
    /// </summary>
    public static Task<T?> FirstOrDefaultAsync<T>(this DbContext context,
        ISpecification<T> specification,
        bool tracking,
        CancellationToken cancellationToken = default)
        where T : class
        => context.Where(specification, tracking).FirstOrDefaultAsync(cancellationToken);

    #endregion
}
