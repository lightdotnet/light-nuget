using Light.EventBus.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Light.EventBus.Abstractions
{
    public interface IEventBus
    {
        Task Publish<T>(T message, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent;
    }
}