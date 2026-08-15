using Microsoft.Graph.Models;
using System.Threading.Tasks;

namespace Light.Graph
{
    public interface IGraphTeams
    {
        Task<ChatCollectionResponse?> GetChatsAsync(string user);
    }
}
