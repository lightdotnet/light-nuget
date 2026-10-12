using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Light.AspNetCore.Cors;

public static class CorsExtensions
{
    /// <summary>
    /// Allow specific origins with any method/header and credentials.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="origins"/> is empty, or contains an empty value or a wildcard (<c>*</c>),
    /// which must not be combined with credentials.
    /// </exception>
    public static void AllowOrigins(this CorsOptions options, string policyName, params string[] origins)
    {
        ValidateOrigins(origins);

        options.AddPolicy(policyName, policy =>
        {
            policy
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    }

    public static void AllowAnyOrigins(this CorsOptions options, string policyName) =>
        options.AddPolicy(policyName, policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });

    private static void ValidateOrigins(string[]? origins)
    {
        if (origins is null || origins.Length == 0)
            throw new ArgumentException("At least one origin is required.", nameof(origins));

        foreach (var origin in origins)
        {
            if (string.IsNullOrWhiteSpace(origin))
                throw new ArgumentException("Origins must not contain empty values.", nameof(origins));

            if (origin.Contains('*'))
                throw new ArgumentException(
                    $"Wildcard origin '{origin}' is not allowed together with credentials. Use AllowAnyOrigins() or list explicit origins.",
                    nameof(origins));
        }
    }
}
