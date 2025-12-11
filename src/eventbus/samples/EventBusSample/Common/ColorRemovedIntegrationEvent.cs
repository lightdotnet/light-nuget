using Light.EventBus.Events;

namespace EventBusSample.Common
{
    [BindingName("color-value-changed")]
    public record ColorRemovedIntegrationEvent(string Color) : EventBase;
}
