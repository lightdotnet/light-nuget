namespace Light.Domain.Entities.Interfaces;

/// <summary>
/// Indicates that the entity supports soft deletion, allowing it to be marked as deleted without being physically removed from the database.
/// </summary>
public interface ISoftDelete
{
    DateTimeOffset? Deleted { get; set; }

    string? DeletedBy { get; set; }
}