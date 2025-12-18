using Light.Domain.Entities.Default;
using Light.Domain.Entities.Interfaces;

namespace Light.Extensions.DynamicObject;

public abstract class DynamicEntity : Entity, IHasAuditTime
{
    public virtual string ObjectName { get; set; } = null!;

    public virtual string PropName { get; set; } = null!;

    public virtual string PropType { get; set; } = null!;

    public virtual string? PropValue { get; set; }

    public virtual DateTimeOffset Created { get; set; } = DateTimeOffset.Now;

    public virtual DateTimeOffset? LastModified { get; set; }
}