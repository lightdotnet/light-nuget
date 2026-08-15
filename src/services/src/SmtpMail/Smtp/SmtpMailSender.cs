using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Smtp
{
    public class SmtpMailSender : SmtpConnection, ISmtpMailSender
    {
        public SmtpMailSender(string host, int port = 25)
        {
            Host = host;
            Port = port;
        }

        public Task SendAsync(
            string from,
            string fromDisplayName,
            List<string> recipients,
            string subject,
            string content,
            List<string>? cc = null,
            List<string>? bcc = null,
            Dictionary<string, byte[]>? attachments = null,
            CancellationToken cancellationToken = default)
        {
            var message = new System.Net.Mail.MailMessage
            {
                From = new MailAddress(from, fromDisplayName),
                Subject = subject,
                IsBodyHtml = true,
                Body = content,
            };

            // add address mail to send
            foreach (var address in recipients)
            {
                message.To.Add(new MailAddress(address));
            }

            if (cc != null)
            {
                // add CC
                foreach (var address in cc)
                {
                    message.CC.Add(new MailAddress(address));
                }
            }

            if (bcc != null)
            {
                // add BCC
                foreach (var address in bcc)
                {
                    message.Bcc.Add(new MailAddress(address));
                }
            }

            if (attachments != null)
            {
                // add attachments
                foreach (var attachment in attachments)
                {
                    message.Attachments.Add(
                        new Attachment(
                            new MemoryStream(attachment.Value),
                            attachment.Key));
                }
            }

            using var smtpClient = new SmtpClient(Host, Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = UseSsl
            };

            using var cancellationRegistration = cancellationToken.Register(smtpClient.SendAsyncCancel);

            return smtpClient.SendMailAsync(message);
        }
    }
}
