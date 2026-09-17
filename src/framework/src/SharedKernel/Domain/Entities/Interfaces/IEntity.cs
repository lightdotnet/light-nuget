namespace Light.Domain.Entities.Interfaces;

/// <summary>
/// Marker interface for entities in the domain model.
/// </summary>
public interface IEntity
{ }

/// <summary>
/// Represents an entity with a unique identifier of type <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IEntity<TKey> : IEntity
{
    TKey Id { get; }
}

