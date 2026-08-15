namespace Light.Repositories;

/// <summary>
///     Use to query and save instances of T with Repository patterns with specify DbContext
/// </summary>
public interface IUnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{ }