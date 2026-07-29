using Light.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Graph
{
    public interface IGraphMailService
    {
        Task SendAsync(MailFrom from, MailMessage mail, CancellationToken cancellationToken = default);
    }
}