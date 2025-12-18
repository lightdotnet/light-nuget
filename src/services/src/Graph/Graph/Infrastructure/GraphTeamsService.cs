using Microsoft.Graph;
using System.Threading.Tasks;

namespace Light.Graph.Infrastructure
{
    internal class GraphTeamsService : IGraphTeams
    {
        private readonly GraphServiceClient _graphServiceClient;

        public GraphTeamsService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task<object?> GetByAsync(string user)
        {
            // Get the list of teams
            var teams = await _graphServiceClient.Users[user].Chats.GetAsync();
            return teams;
        }
    }
}
