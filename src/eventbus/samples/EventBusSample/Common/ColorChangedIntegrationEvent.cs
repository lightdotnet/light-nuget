using Light.EventBus.Events;
using MassTransit;

namespace EventBusSample.Common
{
    [BindingName("color-value-changed")]
    [MessageUrn("urn:color-value-changed", useDefaultPrefix: false)]
    public record ColorChangedIntegrationEvent : EventBase
    {
        public string OldColor { get; set; } = null!;

        public string NewColor { get; set; } = null!;

        public DateTime ChangeOn { get; set; }
    }
}
