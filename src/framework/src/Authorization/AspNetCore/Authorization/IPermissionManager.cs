namespace Light.AspNetCore.Authorization;

public interface IPermissionManager
{
    IReadOnlyCollection<PermissionDefinition> GetPermissions();
}
