using MassTransit;

namespace Light.MassTransit.RabbitMQ
{
    /// <summary>
    /// Consumer definition whose receive endpoint (queue) is named after the message's
    /// <see cref="Light.EventBus.Events.BindingNameAttribute"/> instead of MassTransit's default consumer-type-based name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Competing consumers across services:</b> with the parameterless constructor the queue name is exactly the
    /// binding name. Every service that consumes the same event through such a definition therefore binds to the
    /// <b>same queue</b> and the services compete for messages — each event is delivered to only one of them
    /// (load-balancing), not to all of them (fan-out). This is correct for multiple instances of one service, but not
    /// for different services that each need their own copy of the event.
    /// </para>
    /// <para>
    /// To give each service its own queue, pass a per-service prefix via
    /// <see cref="ConsumerDefinition{TMessage, TConsumer}(string)"/> (e.g. <c>base("billing")</c> produces the queue
    /// <c>billing-color-value-changed</c>), or set <c>EndpointName</c> yourself in the derived constructor. The
    /// prefix only applies when the message carries a binding name; otherwise MassTransit's endpoint name formatter
    /// decides the name, as before.
    /// </para>
    /// </remarks>
    public abstract class ConsumerDefinition<TMessage, TConsumer> : ConsumerDefinition<TConsumer>
        where TMessage : class
        where TConsumer : class, IConsumer<TMessage>
    {
        protected ConsumerDefinition()
            : this(null)
        {
        }

        /// <param name="endpointNamePrefix">
        /// Optional per-service prefix for the queue name. When not null/whitespace and <typeparamref name="TMessage"/>
        /// has a binding name, the endpoint name becomes <c>{endpointNamePrefix}-{bindingName}</c>.
        /// Allowed characters (RabbitMQ entity names): letters, digits, <c>-</c>, <c>_</c>, <c>.</c>, <c>:</c>.
        /// </param>
        protected ConsumerDefinition(string? endpointNamePrefix)
        {
            if (!string.IsNullOrWhiteSpace(endpointNamePrefix)
                && !System.Text.RegularExpressions.Regex.IsMatch(endpointNamePrefix!.Trim(), "^[A-Za-z0-9._:-]+$"))
            {
                throw new System.ArgumentException(
                    $"Endpoint name prefix '{endpointNamePrefix}' contains invalid characters. Allowed: letters, digits, '-', '_', '.', ':'.",
                    nameof(endpointNamePrefix));
            }

            var displayName = typeof(TMessage).GetBindingName();
            if (!string.IsNullOrEmpty(displayName))
            {
                EndpointName = string.IsNullOrWhiteSpace(endpointNamePrefix)
                    ? displayName
                    : $"{endpointNamePrefix!.Trim()}-{displayName}";
            }
        }
    }
}
