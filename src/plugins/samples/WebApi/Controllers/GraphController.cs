using Light.Graph;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Microsoft Graph demo. The Graph services are only registered when the "Graph" configuration section
    /// has TenantId/ClientId/ClientSecret (see Program.cs); otherwise these endpoints return 503.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class GraphController(IServiceProvider serviceProvider) : ControllerBase
    {
        private const string NotConfiguredMessage =
            "Microsoft Graph is not configured. Set Graph:TenantId, Graph:ClientId and Graph:ClientSecret (e.g. via dotnet user-secrets).";

        [HttpGet("send_email")]
        public async Task<IActionResult> SendMail()
        {
            var graphMailService = serviceProvider.GetService<IGraphMailService>();
            if (graphMailService is null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, NotConfiguredMessage);
            }

            await graphMailService.SendAsync(
                "test@yopmail.com",
                ["test@yopmail.com"],
                "Test",
                "Test Body"
            );

            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> Get(string user)
        {
            var graphTeams = serviceProvider.GetService<IGraphTeams>();
            if (graphTeams is null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, NotConfiguredMessage);
            }

            var res = await graphTeams.GetChatsAsync(user);

            return Ok(res);
        }
    }
}
