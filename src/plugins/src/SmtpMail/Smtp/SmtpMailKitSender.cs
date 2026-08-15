using MailKit.Net.Smtp;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Smtp
{
    public class SmtpMailKitSender : SmtpConnection, ISmtpMailSender
    {
        public string UserName { get; protected set; }

        public string Password { get; protected set; }

        public SmtpMailKitSender(string host, string username, string password, int port = 587)
        {
            Host = host;
            Port = port;

            UserName = username;
            Password = password;
        }

        public async Task SendAsync(
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
            var email = new MimeMessage
            {
                Sender = new MailboxAddress(fromDisplayName, from),
                Subject = subject,
            };

            var bodyBuilder = new BodyBuilder { HtmlBody = content };

            // add address mail to send
            foreach (var address in recipients)
            {
                email.To.Add(MailboxAddress.Parse(address));
            }

            if (cc != null)
            {
                // add CC
                email.Cc.AddRange(cc.Select(s => MailboxAddress.Parse(s)));
            }

            if (bcc != null)
            {
                // add BCC
                email.Bcc.AddRange(bcc.Select(s => MailboxAddress.Parse(s)));
            }

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    // file from stream
                    bodyBuilder.Attachments.Add(attachment.Key, attachment.Value);
                }
            }

            // build message body
            email.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(Host, Port, UseSsl, cancellationToken);
            await smtpClient.AuthenticateAsync(UserName, Password, cancellationToken);
            await smtpClient.SendAsync(email, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
        }
    }
}