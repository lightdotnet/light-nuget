using Light.EventBus.Abstractions;
using Light.EventBus.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.MassTransit.RabbitMQ
{
    public class RabbitMQEventBus : IEventBus
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<RabbitMQEventBus> _logger;

        public RabbitMQEventBus(
            IPublishEndpoint publishEndpoint,
            ILogger<RabbitMQEventBus> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
        public async Task Publish<T>(T message, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await _publishEndpoint.Publish(message, cancellationToken);

            _logger.LogInformation("event_bus {id} published", message.Id);

            _logger.LogDebug("event_bus {id} published with data: {@Data}",
                message.Id,
                message);
        }
    }
}
