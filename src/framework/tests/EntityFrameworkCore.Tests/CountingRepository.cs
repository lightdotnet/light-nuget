using Light.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Tests;

// Test-only repository whose constructor has an observable side effect (a counter) and an
// artificial delay, used to widen the race window in UnitOfWork.Set<T>() concurrency tests.
public class CountingRepository<TEntity> : Repository<TEntity> where TEntity : class
{
    public static int ConstructedCount;

    public CountingRepository(DbContext context) : base(context)
    {
        Thread.Sleep(10);
        Interlocked.Increment(ref ConstructedCount);
    }
}
