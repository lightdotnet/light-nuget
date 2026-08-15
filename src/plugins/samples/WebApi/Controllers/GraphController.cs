using Light.Graph;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GraphController(
        IGraphMailService graphMailService,
        IGraphTeams graphTeams) : ControllerBase
    {
        [HttpGet("send_email")]
        public async Task<IActionResult> SendMail()
        {
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
            var res = await graphTeams.GetChatsAsync(user);

            return Ok(res);
        }
    }
}