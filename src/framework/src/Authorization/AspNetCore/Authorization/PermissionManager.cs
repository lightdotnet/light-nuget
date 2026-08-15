namespace Light.AspNetCore.Authorization;

public sealed class PermissionManager : IPermissionManager
{
    private readonly PermissionRegistry _registry;

    public PermissionManager(
        IEnumerable<IPermissionDefinitionProvider> providers)
    {
        _registry = new PermissionRegistry();

        foreach (var provider in providers)
        {
            foreach (var permission in provider.Define())
            {
                _registry.Add(permission);
            }
        }
    }

    public IReadOnlyCollection<PermissionDefinition> GetPermissions()
    {
        return _registry.GetAll();
    }
}