using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Light.AspNetCore.Cors;

public static class CorsExtensions
{
    public static void AllowOrigins(this CorsOptions options, string policyName, params string[] origins) =>
        options.AddPolicy(policyName, policy =>
        {
            policy
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });

    public static void AllowAnyOrigins(this CorsOptions options, string policyName) =>
        options.AddPolicy(policyName, policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
}
