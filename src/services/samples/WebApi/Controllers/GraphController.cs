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
                new Light.Mail.MailFrom("test@yopmail.com"),
                new Light.Mail.MailMessage
                {
                    Recipients = ["test@yopmail.com"],
                    Subject = "Test",
                    Content = "Test Body"
                }
            );

            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> Get(string user)
        {
            var res = await graphTeams.GetByAsync(user);

            return Ok(res);
        }
    }
}