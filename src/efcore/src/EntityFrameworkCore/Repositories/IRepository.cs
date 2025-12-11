namespace Light.Repositories;

/// <summary>
///     Can be used to query, add, update, remove instances of T with a specify DbContext
/// </summary>
public interface IRepository<TEntity, TContext> : IRepository<TEntity>
    where TEntity : class
    where TContext : DbContext
{ }