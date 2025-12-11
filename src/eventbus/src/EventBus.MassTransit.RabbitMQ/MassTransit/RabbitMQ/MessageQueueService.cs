using Light.EventBus.Abstractions;
using Light.EventBus.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Light.MassTransit.RabbitMQ
{
    public class MessageQueueService : IEventBus
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<MessageQueueService> _logger;

        public MessageQueueService(
            IPublishEndpoint publishEndpoint,
            ILogger<MessageQueueService> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Publish<T>(T message, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent
        {
            await _publishEndpoint.Publish(message, cancellationToken);

            _logger.LogInformation("event_bus {id} published with data: {@Data}",
                message.Id,
                message);
        }
    }
}
