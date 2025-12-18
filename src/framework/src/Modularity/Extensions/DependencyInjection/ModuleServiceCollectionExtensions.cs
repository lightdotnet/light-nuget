using Light.AspNetCore.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Light.Extensions.DependencyInjection;

public static class ModuleServiceCollectionExtensions
{
    /// <summary>
    /// Scan & add module services with IConfiguration
    /// </summary>
    public static IServiceCollection AddModules<T>(this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] assemblies)
        where T : IModuleServiceCollection
    {
        // get all classes inherit from interface
        var moduleServices = AsemblyTypeExtensions.GetAssignableFrom<T>(assemblies)
            .Select(s => Activator.CreateInstance(s) as IModuleServiceCollection);

        foreach (var instance in moduleServices)
        {
            instance?.Add(services);
            instance?.Add(services, configuration);
        }

        return services;
    }

    /// <summary>
    /// Scan & add module services with IConfiguration
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] assemblies) =>
        services.AddModules<AppModule>(configuration, assemblies);
}