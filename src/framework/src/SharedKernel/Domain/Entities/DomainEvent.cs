using Light.Mediator;

namespace Light.Domain.Entities;

/// <summary>
///     A base type for domain events. Depends on MediatR INotification.
///     Includes DateOccurred which is set on creation.
/// </summary>
public abstract record DomainEvent : INotification
{
    public virtual DateTime TriggeredOn { get; protected set; } = DateTime.UtcNow;
}