namespace Light.Domain.Entities.Interfaces;

/// <summary>
/// Represents an entity that can raise domain events.
/// </summary>
public interface IEvent
{
    IReadOnlyCollection<BaseEvent> DomainEvents { get; }

    void AddDomainEvent(BaseEvent domainEvent);

    void RemoveDomainEvent(BaseEvent domainEvent);

    void ClearDomainEvents();
}