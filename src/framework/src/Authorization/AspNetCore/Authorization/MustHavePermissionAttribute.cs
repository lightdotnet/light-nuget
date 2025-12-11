using Microsoft.AspNetCore.Authorization;

namespace Light.AspNetCore.Authorization;

public class MustHavePermissionAttribute : AuthorizeAttribute
{
    public MustHavePermissionAttribute(string policy) => Policy = policy;
}