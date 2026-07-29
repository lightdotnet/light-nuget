using Light.Mail;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Light.SmtpMail
{
    public class SmtpNetMailSender : SmtpConnection, ISmtpMailSender
    {
        public SmtpNetMailSender(string host, int port = 25)
        {
            Host = host;
            Port = port;
        }

        public async Task SendAsync(MailFrom from, Mail.MailMessage mail, CancellationToken cancellationToken = default)
        {
            var message = new System.Net.Mail.MailMessage
            {
                From = new MailAddress(from.Address, from.DisplayName),
                Subject = mail.Subject,
                IsBodyHtml = true,
                Body = mail.Content,
            };

            // add address mail to send
            foreach (var address in mail.Recipients)
            {
                message.To.Add(new MailAddress(address));
            }

            if (mail.CcRecipients != null)
            {
                // add CC
                foreach (var address in mail.CcRecipients)
                {
                    message.CC.Add(new MailAddress(address));
                }
            }

            if (mail.BccRecipients != null)
            {
                // add BCC
                foreach (var address in mail.BccRecipients)
                {
                    message.Bcc.Add(new MailAddress(address));
                }
            }

            if (mail.Attachments != null)
            {
                // add attachments
                foreach (var attachment in mail.Attachments)
                {
                    message.Attachments.Add(new Attachment(new MemoryStream(attachment.FileToBytes), attachment.FileName));
                }
            }

            using var smtpClient = new SmtpClient(Host, Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = UseSsl
            };

            using var cancellationRegistration = cancellationToken.Register(smtpClient.SendAsyncCancel);

            await smtpClient.SendMailAsync(message);
        }
    }
}
