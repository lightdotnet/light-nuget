namespace Light.Domain.Entities;

/// <summary>
///     A base type for domain events.
///     Includes <see cref="TriggeredOn"/> which is set (UTC) on creation.
/// </summary>
public abstract record BaseEvent
{
    public virtual DateTimeOffset TriggeredOn { get; protected set; } = DateTimeOffset.UtcNow;
}