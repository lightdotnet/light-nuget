using Light.Smtp;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly ILogger<MailController> _logger;

        public MailController(ILogger<MailController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var host = "smtp.ethereal.email";
            var userName = "jermain.torphy@ethereal.email";
            var password = "GHMdV12nF7zfFhqG7Z";

            var smtpClient = new SmtpMailKitSender(host, userName, password)
            {
                UseSsl = false,
            };

            await smtpClient.SendAsync(
                "leslie.bailey@ethereal.email",
                "leslie.bailey@ethereal.email",
                ["test@yopmail.com"],
                "Test...." + DateTime.Now,
                "Hello,.......... this test mail");

            return Ok();
        }
    }
}