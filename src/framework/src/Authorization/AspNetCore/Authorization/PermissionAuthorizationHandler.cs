using Microsoft.AspNetCore.Authorization;

namespace Light.AspNetCore.Authorization;

public abstract class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>;