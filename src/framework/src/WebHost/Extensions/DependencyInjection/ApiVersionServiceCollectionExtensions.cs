using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Extensions.DependencyInjection;

public static class ApiVersionServiceCollectionExtensions
{
    /// <summary>
    /// add API version
    /// </summary>
    public static IApiVersioningBuilder AddApiVersion(
        this IServiceCollection services,
        int version, int minorVersion = 0,
        bool groupByNameAndVersion = true)
        => services
        .AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(version, minorVersion);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        })
        .AddApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
            o.FormatGroupName = (groupName, apiVersion) => groupByNameAndVersion ? $"{groupName}_{apiVersion}" : apiVersion;
        });
}
