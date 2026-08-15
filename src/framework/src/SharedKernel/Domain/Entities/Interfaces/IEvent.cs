namespace Light.Domain.Entities.Interfaces;

public interface IEvent
{
    IReadOnlyCollection<BaseEvent> DomainEvents { get; }

    void AddDomainEvent(BaseEvent domainEvent);

    void RemoveDomainEvent(BaseEvent domainEvent);

    void ClearDomainEvents();
}