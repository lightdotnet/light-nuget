namespace Light.AspNetCore.Authorization;

public sealed class PermissionRegistry
{
    private readonly Dictionary<string, PermissionDefinition> _permissions = [];

    public void Add(PermissionDefinition permission)
    {
        _permissions.TryAdd(permission.Name, permission);
    }

    public IReadOnlyCollection<PermissionDefinition> GetAll()
    {
        return [.. _permissions.Values];
    }

    public PermissionDefinition? Find(string name)
    {
        _permissions.TryGetValue(name, out var permission);

        return permission;
    }
}