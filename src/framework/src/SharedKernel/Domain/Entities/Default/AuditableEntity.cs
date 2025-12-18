namespace Light.Domain.Entities.Default;

/// <summary>
///     A base class for DDD Auditable Entities. Includes support for domain events dispatched post-persistence.
///     use string for type of ID and set default is NewGuid as string.
/// </summary>
public abstract class AuditableEntity : AuditableEntity<string>
{
    protected AuditableEntity() => Id = LightId.NewId();
}
