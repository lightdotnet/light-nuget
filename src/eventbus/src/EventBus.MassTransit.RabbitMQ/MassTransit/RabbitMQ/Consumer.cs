using Light.EventBus.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Light.MassTransit.RabbitMQ
{
    public abstract class Consumer<TMessage> : IConsumer<TMessage>
         where TMessage : class, IIntegrationEvent
    {
        private readonly ILogger _logger;

        // for inherit classes can config this;
        public virtual bool ThrowIfError => true;

        public Consumer(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TMessage> context)
        {
            var message = context.Message;

            try
            {
                await Handle(message);

                _logger.LogInformation("event_bus {id} consumed data: {@Data}",
                    message.Id,
                    message);
            }
            catch (Exception ex)
            {
                _logger.LogError("event_bus {id} consumed data: {@Data} with error: {error}",
                    message.Id,
                    message,
                    ex.Message);

                if (ThrowIfError)
                {
                    throw ex;
                }
            }
        }

        public abstract Task Handle(TMessage message);
    }
}
