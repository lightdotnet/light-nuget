using System;

namespace Light.EventBus.Events
{
    public interface IIntegrationEvent
    {
        string Id { get; }

        DateTime CreationDate { get; }
    }
}
