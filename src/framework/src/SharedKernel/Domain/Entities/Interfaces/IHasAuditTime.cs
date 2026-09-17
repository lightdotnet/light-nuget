namespace Light.Domain.Entities.Interfaces;

/// <summary>
/// Indicates that the entity has audit time properties (creation and last modification time).
/// </summary>
public interface IHasAuditTime : IHasCreationTime, IHasModificationTime
{ }

/// <summary>
/// Indicates that the entity has a creation time property.
/// </summary>
public interface IHasCreationTime : IHasAudit
{
    DateTimeOffset Created { get; set; }
}

/// <summary>
/// Indicates that the entity has a last modification time property.
/// </summary>
public interface IHasModificationTime : IHasAudit
{
    DateTimeOffset? LastModified { get; set; }
}

