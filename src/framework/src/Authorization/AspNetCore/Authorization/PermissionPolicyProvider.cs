using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Light.AspNetCore.Authorization;

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policyCache = new();

    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; } = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public virtual async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (_policyCache.TryGetValue(policyName, out var cachedPolicy))
        {
            return cachedPolicy;
        }

        // normally-registered policies (e.g. AddAuthorization(o => o.AddPolicy(...))) take precedence over permissions
        var registeredPolicy = await FallbackPolicyProvider.GetPolicyAsync(policyName);
        if (registeredPolicy is not null)
        {
            return registeredPolicy;
        }

        if (await CheckPermissionValidAsync(policyName) is true)
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(policyName));
            var builtPolicy = policy.Build();

            _policyCache[policyName] = builtPolicy;
            return builtPolicy;
        }

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

    // honor AuthorizationOptions.FallbackPolicy configured by the consumer
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();
}