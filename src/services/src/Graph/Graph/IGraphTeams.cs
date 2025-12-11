using System.Threading.Tasks;

namespace Light.Graph
{
    public interface IGraphTeams
    {
        Task<object?> GetByAsync(string user);
    }
}
