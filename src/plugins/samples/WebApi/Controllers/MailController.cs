using Light.Smtp;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly ILogger<MailController> _logger;
        private readonly IConfiguration _configuration;

        public MailController(ILogger<MailController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // credentials come from the "SMTP" section (use user-secrets / env vars, never commit them)
            var host = _configuration["SMTP:Host"]!;
            var userName = _configuration["SMTP:UserName"]!;
            var password = _configuration["SMTP:Password"]!;

            var smtpClient = new SmtpMailKitSender(host, userName, password)
            {
                UseSsl = false,
            };

            await smtpClient.SendAsync(
                userName,
                userName,
                ["test@yopmail.com"],
                "Test...." + DateTime.Now,
                "Hello,.......... this test mail");

            return Ok();
        }
    }
}