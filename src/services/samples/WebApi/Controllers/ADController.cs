using Light.ActiveDirectory.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ADController(IActiveDirectoryService activeDirectoryService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(string user)
        {
            return Ok(await activeDirectoryService.GetByUserNameAsync(user));
        }

        [HttpGet("check_password")]
        public async Task<IActionResult> CheckPassword(string user, string password)
        {
            return Ok(await activeDirectoryService.CheckPasswordSignInAsync(user, password));
        }
    }
}
