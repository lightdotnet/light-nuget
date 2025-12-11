using Light.MassTransit.RabbitMQ;
using MassTransit;

namespace EventBusSample.Common;

public class ColorRemovedConsumer(
    ILogger<ColorRemovedConsumer> logger) :
    Consumer<ColorRemovedIntegrationEvent>(logger)
{
    public override bool ThrowIfError => false;

    public override async Task Handle(ColorRemovedIntegrationEvent message)
    {
        await Task.Delay(2000);

        logger.LogInformation("Color removed {color} by {id}", message.Color, message.Id);

        //throw new Exception("Color changed error when empty");
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
        // configure message retry with millisecond intervals
        configurator.UseMessageRetry(r => r.Intervals(100, 200, 5000, 8000, 10000));

        // use the outbox to prevent duplicate events from being published
        configurator.UseInMemoryOutbox(context);
    }
}
