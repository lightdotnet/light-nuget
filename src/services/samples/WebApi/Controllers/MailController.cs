using Light.Mail;
using Light.SmtpMail;
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
            //string filePath = @$"D:\\backups\\pexels-pixabay-268533.jpg";
            //byte[] byteArray = System.IO.File.ReadAllBytes(filePath);

            var from = new MailFrom("leslie.bailey@ethereal.email");

            var message = new MailMessage
            {
                Subject = "Test...." + DateTime.Now,
                Content = "Hello,.......... this test mail",
            };

            message.Recipients = ["test@yopmail.com"];

            //message.CcRecipients.Add("test1@yopmail.com");

            //message.BccRecipients.Add("test2@yopmail.com");

            //message.Attachments.Add(new MailAttachment
            //{
            //    FileName = "pexels-pixabay-268533.jpg",
            //    FileToBytes = byteArray
            //});

            var host = "smtp.ethereal.email";
            var userName = "jermain.torphy@ethereal.email";
            var password = "GHMdV12nF7zfFhqG7Z";

            var smtpClient = new SmtpMailKit(host, userName, password)
            {
                UseSsl = false,
            };

            await smtpClient.SendAsync(from, message);

            return Ok();
        }
    }
}