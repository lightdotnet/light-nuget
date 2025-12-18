using Light.Domain.Entities.Interfaces;

namespace Light.Domain.Entities;

/// <summary>
///     A base class for DDD Auditable Entities. Includes support for domain events dispatched post-persistence.
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable
{
    public virtual DateTimeOffset Created { get; set; }

    public virtual string? CreatedBy { get; set; }

    public virtual DateTimeOffset? LastModified { get; set; }

    public virtual string? LastModifiedBy { get; set; }
}

/// <summary>
///     A base class for DDD Auditable Entities. Includes support for domain events dispatched post-persistence.
///     support both GUID and int IDs, change to EntityBase and use TId as the type for Id.
/// </summary>
public abstract class AuditableEntity<TId> : AuditableEntity, IEntity<TId>
{
    public virtual TId Id { get; set; } = default!;
}
