namespace Light.AspNetCore.Authorization;

public interface IPermissionDefinitionProvider
{
    IEnumerable<PermissionDefinition> Define();
}