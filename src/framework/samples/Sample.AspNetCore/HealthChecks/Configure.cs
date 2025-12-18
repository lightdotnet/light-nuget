using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Sample.AspNetCore.HealthChecks;

public static class Configure
{
    public static IServiceCollection AddHealthChecksService(this IServiceCollection services)
    {
        services.AddHealthChecks();

        return services;
    }

    public static IEndpointRouteBuilder MapHealthChecksEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/hc", new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}