using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Graph
{
    public interface IGraphMailService
    {
        Task SendAsync(
            string from,
            List<string> recipients,
            string subject,
            string content,
            List<string>? ccRecipients = null,
            List<string>? bccRecipients = null,
            Dictionary<string, byte[]>? attachments = null,
            CancellationToken cancellationToken = default);
    }
}