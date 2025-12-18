using Light.ActiveDirectory.Interfaces;
using Light.ActiveDirectory.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Versioning;

namespace Light.ActiveDirectory;

public static class DependencyInjection
{
    public static IServiceCollection AddActiveDirectory(this IServiceCollection services)
    {
        services.AddTransient<IActiveDirectoryService, FakeActiveDirectoryService>();

        return services;
    }

    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddActiveDirectory(this IServiceCollection services, Action<DomainOptions> action)
    {
        //services.Configure(action);

        var options = new DomainOptions();
        action.Invoke(options);

        services.AddTransient<IActiveDirectoryService>(sp =>
        {
            return new ActiveDirectoryService(options);
        });

        return services;
    }

    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddLdapActiveDirectory(this IServiceCollection services, Action<LdapOptions> action)
    {
        services.Configure(action);

        services.AddTransient<IActiveDirectoryService>(sp =>
        {
            var options = sp.GetRequiredService<LdapOptions>();
            return new LDAPService(options);
        });

        return services;
    }
}