using MassTransit;

namespace Light.MassTransit.RabbitMQ
{
    public class ConsumerDefinition<TMessage, TConsumer> : ConsumerDefinition<TConsumer>
        where TMessage : class
        where TConsumer : class, IConsumer<TMessage>
    {
        protected ConsumerDefinition()
        {
            var displayName = typeof(TMessage).GetBindingName();
            if (!string.IsNullOrEmpty(displayName))
            {
                EndpointName = displayName;
            }
        }
    }
}
