using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Use default PermissionPolicyProvider
    /// </summary>
    public static IServiceCollection AddPermissionPolicyProvider(this IServiceCollection services) =>
        services.AddPermissionPolicyProvider<PermissionPolicyProvider>();

    /// <summary>
    /// Use custom PermissionPolicyProvider
    /// </summary>
    public static IServiceCollection AddPermissionPolicyProvider<T>(this IServiceCollection services)
        where T : PermissionPolicyProvider
    {
        services.AddSingleton<IAuthorizationPolicyProvider, T>();
        return services;
    }

    public static IServiceCollection AddPermissionAuthorizationHandler<T>(this IServiceCollection services)
        where T : PermissionAuthorizationHandler
    {
        services.AddScoped<IAuthorizationHandler, T>();

        return services;
    }
}