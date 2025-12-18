using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Light.AspNetCore.Swagger;

internal static class SwaggerSecuritySchemeExtensions
{
    internal static SwaggerGenOptions AddJwtSecurityScheme(this SwaggerGenOptions swaggerGenOptions)
    {
        var scheme = "bearer";

        // Include 'SecurityScheme' to use JWT Authentication
        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            Scheme = scheme,
            BearerFormat = "JWT",
            Name = "JWT Authentication",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",
        };

        AddSecurity(swaggerGenOptions, scheme, jwtSecurityScheme);

        return swaggerGenOptions;
    }

    internal static SwaggerGenOptions AddBasicSecurityScheme(this SwaggerGenOptions swaggerGenOptions)
    {
        var scheme = "basic";

        // Include 'SecurityScheme' to use Basic Authentication
        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            Scheme = scheme,
            Name = "Basic Authentication",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "Put your basic Authentication on textbox below!",
        };

        AddSecurity(swaggerGenOptions, scheme, jwtSecurityScheme);

        return swaggerGenOptions;
    }

    private static void AddSecurity(SwaggerGenOptions swaggerGenOptions, string name, OpenApiSecurityScheme openApiSecurityScheme)
    {
        swaggerGenOptions.AddSecurityDefinition(name, openApiSecurityScheme);

        swaggerGenOptions.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(name, doc),
                new List<string>()
            }
        });
    }
}
