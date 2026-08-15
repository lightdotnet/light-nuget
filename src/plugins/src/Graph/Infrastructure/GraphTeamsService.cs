using Light.Graph;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Threading.Tasks;

namespace Light.Infrastructure
{
    internal class GraphTeamsService : IGraphTeams
    {
        private readonly GraphServiceClient _graphServiceClient;

        public GraphTeamsService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task<ChatCollectionResponse?> GetChatsAsync(string user)
        {
            // Get the list of teams
            return await _graphServiceClient.Users[user].Chats.GetAsync();
        }
    }
}
