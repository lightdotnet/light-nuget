using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Light.AspNetCore.Swagger;

public static class SwaggerServiceCollectionExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var sectionName = "Swagger";

        services.Configure<SwaggerOptions>(configuration.GetSection(sectionName));

        var settings = configuration.GetSection(sectionName).Get<SwaggerOptions>();

        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        if (settings.Enable)
        {
            if (settings.VersionDefinition)
            {
                services.AddTransient<IConfigureOptions<SwaggerGenOptions>, VersionDefinitionSwaggerOptions>();
            }

            services.AddSwaggerGen(opt =>
            {
                if (!string.IsNullOrEmpty(settings.SecurityScheme))
                {
                    if (settings.SecurityScheme.Contains("jwt"))
                    {
                        opt.AddJwtSecurityScheme();
                    }

                    if (settings.SecurityScheme.Contains("basic"))
                    {
                        opt.AddBasicSecurityScheme();
                    }
                }

                opt.CustomSchemaIds(x => x.FullName); // fix Swagger when contain multi model, dto has same name

                opt.DocumentFilter<TitleFilter>();

                opt.UseInlineDefinitionsForEnums();
            });

            services.AddTransient<IConfigureOptions<SwaggerUIOptions>, CustomSwaggerUIOptions>();
        }

        return services;
    }
}
