using Light.Mediator;

namespace Light.Domain.Entities;

/// <summary>
///     A base type for domain events. Depends on MediatR INotification.
///     Includes DateOccurred which is set on creation.
/// </summary>
public abstract class DomainEvent : INotification
{
    public virtual DateTimeOffset TriggeredOn { get; protected set; } = DateTimeOffset.UtcNow;
}