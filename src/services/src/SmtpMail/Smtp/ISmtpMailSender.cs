using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Smtp
{
    public interface ISmtpMailSender
    {
        Task SendAsync(
            string from,
            string fromDisplayName,
            List<string> recipients,
            string subject,
            string content,
            List<string>? cc = null,
            List<string>? bcc = null,
            Dictionary<string, byte[]>? attachments = null,
            CancellationToken cancellationToken = default);
    }
}
