using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Light.AspNetCore.Authorization;

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; } = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public virtual async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (await CheckPermissionValidAsync(policyName) is true)
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(policyName));
            return policy.Build();
        }

        //return await FallbackPolicyProvider.GetPolicyAsync(policyName);
        return null;
    }

    /// <summary>
    /// default all (include undefined) permissions are valid
    /// if you want to restrict permissions, override this method
    /// </summary>
    public virtual Task<bool> CheckPermissionValidAsync(string policyName)
    {
        return Task.FromResult(true);
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);
}