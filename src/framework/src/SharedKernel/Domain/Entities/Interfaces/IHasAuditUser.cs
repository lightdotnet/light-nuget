namespace Light.Domain.Entities.Interfaces;

public interface IHasAuditUser
{
    string? CreatedBy { get; set; }

    string? LastModifiedBy { get; set; }
}