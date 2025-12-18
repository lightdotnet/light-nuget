using Light.Repositories;
using Light.Specification;
using System.Linq.Expressions;

namespace Light.EntityFrameworkCore.Repositories;

/// <inheritdoc/>
public class Repository<TEntity>(DbContext context) : IRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    /// <inheritdoc/>
    public virtual IQueryable<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        => context.Set<TEntity>().Include(navigationPropertyPath);

    /// <inheritdoc/>
    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression)
        => context.Set<TEntity>().Where(expression);

    /// <inheritdoc/>
    public virtual IQueryable<TEntity> WhereIf(bool condition, Expression<Func<TEntity, bool>> expression)
        => context.Set<TEntity>().WhereIf(condition, expression);

    /// <inheritdoc/>
    public virtual IQueryable<TEntity> Where(ISpecification<TEntity> specification)
        => context.Set<TEntity>().Where(specification);

    /// <inheritdoc/>
    public virtual IQueryable<TEntity> WhereIf(bool condition, ISpecification<TEntity> specification)
        => context.Set<TEntity>().WhereIf(condition, specification);

    /// <inheritdoc/>
    public virtual void Add(TEntity entity) => _dbSet.Add(entity);

    /// <inheritdoc/>
    public virtual void AddRange(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);

    /// <inheritdoc/>
    public virtual void Update(TEntity entity) => _dbSet.Update(entity);

    /// <inheritdoc/>
    public virtual void UpdateRange(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);

    /// <inheritdoc/>
    public virtual void Remove(TEntity entity) => _dbSet.Remove(entity);

    /// <inheritdoc/>
    public virtual void RemoveRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

    /// <inheritdoc/>
    public virtual int SaveChanges() => context.SaveChanges();

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public virtual async Task<TEntity?> FindAsync<TKey>(TKey key, CancellationToken cancellationToken = default) where TKey : notnull
        => await context.FindAsync<TEntity>(key, cancellationToken);

    /// <inheritdoc/>
    public virtual async Task<TEntity?> FindAsync(object?[] key, CancellationToken cancellationToken = default)
        => await context.FindAsync<TEntity>(key, cancellationToken);

    /// <inheritdoc/>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    /// <inheritdoc/>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(entities, cancellationToken);

    /// <inheritdoc/>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}

/// <inheritdoc/>
public class Repository<TEntity, TContext>(TContext context) :
    Repository<TEntity>(context),
    IRepository<TEntity, TContext>
    where TEntity : class
    where TContext : DbContext;