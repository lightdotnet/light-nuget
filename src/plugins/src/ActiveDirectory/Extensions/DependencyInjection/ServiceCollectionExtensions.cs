using Light.ActiveDirectory;
using Light.ActiveDirectory.Interfaces;
using Light.ActiveDirectory.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Versioning;

namespace Light.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="FakeActiveDirectoryService"/> — a no-op stand-in that always reports
    /// unconfigured/unauthenticated. For a real Active Directory backend use
    /// <see cref="AddActiveDirectory(IServiceCollection, Action{DomainOptions})"/> or
    /// <see cref="AddLdapActiveDirectory(IServiceCollection, Action{LdapOptions})"/> instead.
    /// </summary>
    public static IServiceCollection AddActiveDirectory(this IServiceCollection services)
    {
        services.AddTransient<IActiveDirectoryService, FakeActiveDirectoryService>();

        return services;
    }

    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddActiveDirectory(this IServiceCollection services, Action<DomainOptions> action)
    {
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
        var options = new LdapOptions();
        action.Invoke(options);

        services.AddTransient<IActiveDirectoryService>(sp =>
        {
            return new LDAPService(options);
        });

        return services;
    }
}