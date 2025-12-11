using Light.Graph;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GraphController(IGraphTeams graphTeams) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(string user)
        {
            var res = await graphTeams.GetByAsync(user);

            return Ok(res);
        }
    }
}