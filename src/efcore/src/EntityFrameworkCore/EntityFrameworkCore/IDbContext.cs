using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Light.EntityFrameworkCore;

/// <summary>
///     A DbContext instance represents a session with the database
/// </summary>
public interface IDbContext : IDisposable, IAsyncDisposable
{
    ChangeTracker ChangeTracker { get; }

    DatabaseFacade Database { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}