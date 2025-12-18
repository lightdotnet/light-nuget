namespace Light.EntityFrameworkCore;

/// <summary>
///     Dynamic set entity
/// </summary>
public interface IDbSet
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
}