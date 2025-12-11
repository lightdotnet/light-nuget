namespace Light.Extensions.DynamicObject;

public interface IDynamicEntityRepository<T>
    where T : new()
{
    Task<T?> Get(string objectName);

    Task Update(string objectName, T value);
}
