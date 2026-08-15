using Light.EventBus.Events;

namespace EventBusSample.Common
{
    [BindingName("color-value-removed")]
    public record ColorRemovedIntegrationEvent(string Color) : EventBase;
}
