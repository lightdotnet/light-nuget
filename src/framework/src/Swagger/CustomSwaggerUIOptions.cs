using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Light.AspNetCore.Swagger;

public class CustomSwaggerUIOptions(IOptions<SwaggerOptions> options) : IConfigureOptions<SwaggerUIOptions>
{
    private readonly SwaggerOptions _settings = options.Value;

    public void Configure(SwaggerUIOptions options)
    {
        var title = _settings.Title;

        if (!string.IsNullOrEmpty(title))
        {
            // change Tab name in browser
            options.DocumentTitle = title;
        }
    }
}

