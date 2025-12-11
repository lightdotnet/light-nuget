using Microsoft.AspNetCore.Authorization;

namespace Light.AspNetCore.Authorization;

public record PermissionRequirement(string Permission) : IAuthorizationRequirement;