namespace Light.Domain.Entities.Interfaces;

public interface ISoftDelete
{
    DateTimeOffset? Deleted { get; set; }

    string? DeletedBy { get; set; }
}