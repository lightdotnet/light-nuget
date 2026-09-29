using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Smtp
{
    public class SmtpMailKitSender : SmtpConnection, ISmtpMailSender
    {
        private const int ImplicitTlsPort = 465;

        public string UserName { get; protected set; }

        public string Password { get; protected set; }

        /// <summary>
        /// Optional explicit TLS mode. When set, it overrides the mapping derived from <see cref="SmtpConnection.UseSsl"/>:
        /// <c>UseSsl = true</c> → <see cref="MailKit.Security.SecureSocketOptions.SslOnConnect"/> on port 465, otherwise
        /// <see cref="MailKit.Security.SecureSocketOptions.StartTls"/> (TLS required);
        /// <c>UseSsl = false</c> → <see cref="MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable"/>.
        /// </summary>
        public SecureSocketOptions? SecureSocketOptions { get; set; }

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
            var email = BuildMessage(from, fromDisplayName, recipients, subject, content, cc, bcc, attachments);

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(Host, Port, ResolveSecureSocketOptions(), cancellationToken);

            // anonymous relays: only authenticate when credentials are configured
            if (!string.IsNullOrEmpty(UserName))
            {
                await smtpClient.AuthenticateAsync(UserName, Password, cancellationToken);
            }

            await smtpClient.SendAsync(email, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
        }

        internal SecureSocketOptions ResolveSecureSocketOptions()
        {
            if (SecureSocketOptions.HasValue)
                return SecureSocketOptions.Value;

            if (!UseSsl)
                return MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable;

            return Port == ImplicitTlsPort
                ? MailKit.Security.SecureSocketOptions.SslOnConnect
                : MailKit.Security.SecureSocketOptions.StartTls;
        }

        internal static MimeMessage BuildMessage(
            string from,
            string fromDisplayName,
            List<string> recipients,
            string subject,
            string content,
            List<string>? cc = null,
            List<string>? bcc = null,
            Dictionary<string, byte[]>? attachments = null)
        {
            var fromAddress = new MailboxAddress(fromDisplayName, from);

            var email = new MimeMessage
            {
                Sender = fromAddress,
                Subject = subject,
            };

            email.From.Add(fromAddress);

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

            return email;
        }
    }
}
