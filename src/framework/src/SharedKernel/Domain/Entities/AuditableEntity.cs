namespace Light.Domain.Entities;

/// <summary>
///     Default audit entity using a string-based <see cref="LightId"/> as primary key.
///     Use this unless the entity requires a non-string key type.
/// </summary>
public abstract class AuditableEntity : BaseAuditableEntity<string>
{
    protected AuditableEntity() => Id = LightId.NewId();
}
