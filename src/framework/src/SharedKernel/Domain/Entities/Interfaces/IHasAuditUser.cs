namespace Light.Domain.Entities.Interfaces;

/// <summary>
/// Indicates that the entity has audit user properties (created by and last modified by).
/// </summary>
public interface IHasAuditUser : IHasAudit
{
    string? CreatedBy { get; set; }

    string? LastModifiedBy { get; set; }
}