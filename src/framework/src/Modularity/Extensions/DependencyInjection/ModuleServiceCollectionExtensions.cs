using Light.AspNetCore.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Light.Extensions.DependencyInjection;

public static class ModuleServiceCollectionExtensions
{
    /// <summary>
    /// Scan & add module services with IConfiguration.
    /// Each call creates new module instances (via the parameterless constructor), and <c>UseModules</c> /
    /// <c>MapModuleEndpoints</c> create their own instances too, so instance state set in <c>Add</c> is not
    /// visible in <c>Use</c>/<c>Map</c>. Share state through DI or static members instead.
    /// </summary>
    public static IServiceCollection AddModules<T>(this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] assemblies)
        where T : IModuleServiceCollection
    {
        // get all classes inherit from interface
        var moduleServices = AssemblyTypeExtensions.GetAssignableFrom<T>(assemblies)
            .Select(s => (IModuleServiceCollection)Activator.CreateInstance(s)!);

        foreach (var instance in moduleServices)
        {
            instance.Add(services);
            instance.Add(services, configuration);
        }

        return services;
    }

    /// <summary>
    /// Scan & add module services with IConfiguration.
    /// Each call creates new module instances (via the parameterless constructor), and <c>UseModules</c> /
    /// <c>MapModuleEndpoints</c> create their own instances too, so instance state set in <c>Add</c> is not
    /// visible in <c>Use</c>/<c>Map</c>. Share state through DI or static members instead.
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] assemblies) =>
        services.AddModules<AppModule>(configuration, assemblies);
}