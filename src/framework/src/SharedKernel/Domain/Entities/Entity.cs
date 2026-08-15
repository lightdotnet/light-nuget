namespace Light.Domain.Entities;

/// <summary>
///     Default base entity using a string-based <see cref="LightId"/> as primary key.
///     Use this unless the entity requires a non-string key type.
/// </summary>
public abstract class Entity : BaseEntity<string>
{
    protected Entity() => Id = LightId.NewId();
}

