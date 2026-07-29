using Light.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Light.SmtpMail
{
    public interface ISmtpMailSender
    {
        Task SendAsync(MailFrom from, MailMessage mail, CancellationToken cancellationToken = default);
    }
}
