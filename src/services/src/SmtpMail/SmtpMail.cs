using Light.Mail;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Light.SmtpMail
{
    public class SmtpMail : SmtpConnection
    {
        public SmtpMail(string host, int port = 25)
        {
            Host = host;
            Port = port;
        }

        public async Task SendAsync(MailFrom from, Mail.MailMessage mail)
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

            using var smtpClient = new SmtpClient(Host, Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = UseSsl
            };

            await smtpClient.SendMailAsync(message);
            smtpClient.Dispose();
        }
    }
}
