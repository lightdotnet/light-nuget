using System.Threading;
using System.Threading.Tasks;

namespace Light.Repositories
{
    public interface ISaveChanges
    {
        /// <summary>
        ///     Save changes when add, update, delete instances of T.
        /// </summary>
        int SaveChanges();

        /// <summary>
        ///     Asynchronously save changes when add, update, delete instances of T.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
