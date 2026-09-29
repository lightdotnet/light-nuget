using Light.MassTransit.RabbitMQ;
using MassTransit;

namespace EventBusSample.Common;

public class ColorRemovedConsumer(
    ILogger<ColorRemovedConsumer> logger) :
    Consumer<ColorRemovedIntegrationEvent>(logger)
{
    // log and swallow handler errors: the message is acknowledged even when Handle throws, so no retry policy or
    // _error queue applies to this consumer (none is configured in ColorRemovedConsumerDefinition).
    public override bool ThrowIfError => false;

    public override Task Handle(ColorRemovedIntegrationEvent message) =>
        throw new NotSupportedException("Handle(message, context) is used instead.");

    // the ConsumeContext overload gives access to the CancellationToken, headers, etc.
    protected override async Task Handle(ColorRemovedIntegrationEvent message, ConsumeContext<ColorRemovedIntegrationEvent> context)
    {
        await Task.Delay(2000, context.CancellationToken);

        logger.LogInformation("Color removed {color} by {id}", message.Color, message.Id);
    }
}

internal class ColorRemovedConsumerDefinition :
    ConsumerDefinition<ColorRemovedIntegrationEvent, ColorRemovedConsumer>
{
    public ColorRemovedConsumerDefinition()
    {
        // limit the number of messages consumed concurrently
        // this applies to the consumer only, not the endpoint
        ConcurrentMessageLimit = 10;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator configurator,
        IConsumerConfigurator<ColorRemovedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // no UseMessageRetry here: ColorRemovedConsumer.ThrowIfError is false, so a retry policy would never trigger.

        // use the outbox to prevent duplicate events from being published
        configurator.UseInMemoryOutbox(context);
    }
}
