using Light.AspNetCore.Modularity;
using Light.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Reflection;

namespace Light.AspNetCore.Builder;

public static class ModuleBuilderExtensions
{
    /// <summary>
    /// Scan & configure module pipelines.
    /// Creates new module instances, independent of those used by <c>AddModules</c>/<c>MapModuleEndpoints</c>;
    /// module instance state does not carry across phases.
    /// </summary>
    public static IApplicationBuilder UseModules<T>(this IApplicationBuilder builder, Assembly[] assemblies)
        where T : IModuleBuilder
    {
        // get all classes inherit from interface
        var modulePipelines = AssemblyTypeExtensions.GetAssignableFrom<T>(assemblies)
            .Select(s => (IModuleBuilder)Activator.CreateInstance(s)!);

        foreach (var instance in modulePipelines)
        {
            instance.Use(builder);
        }

        return builder;
    }

    /// <summary>
    /// Scan & configure module pipelines by default.
    /// Creates new module instances, independent of those used by <c>AddModules</c>/<c>MapModuleEndpoints</c>;
    /// module instance state does not carry across phases.
    /// </summary>
    public static IApplicationBuilder UseModules(this IApplicationBuilder builder, Assembly[] assemblies) =>
        builder.UseModules<AppModule>(assemblies);


    /// <summary>
    /// Scan & map module endpoints.
    /// Creates new module instances, independent of those used by <c>AddModules</c>/<c>UseModules</c>;
    /// module instance state does not carry across phases.
    /// </summary>
    public static IEndpointRouteBuilder MapModuleEndpoints<T>(this IEndpointRouteBuilder builder, Assembly[] assemblies)
        where T : IModuleEndpoint
    {
        // get all classes inherit from interface
        var modulePipelines = AssemblyTypeExtensions.GetAssignableFrom<T>(assemblies)
            .Select(s => (IModuleEndpoint)Activator.CreateInstance(s)!);

        foreach (var instance in modulePipelines)
        {
            instance.Map(builder);
        }

        return builder;
    }

    /// <summary>
    /// Scan & map module endpoints by default.
    /// Creates new module instances, independent of those used by <c>AddModules</c>/<c>UseModules</c>;
    /// module instance state does not carry across phases.
    /// </summary>
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder builder, Assembly[] assemblies) =>
        builder.MapModuleEndpoints<AppModule>(assemblies);
}
