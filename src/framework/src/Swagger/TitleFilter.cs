using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Light.AspNetCore.Swagger;

public class TitleFilter(IOptions<SwaggerOptions> options) : IDocumentFilter
{
    private readonly SwaggerOptions _settings = options.Value;

    public void Apply(OpenApiDocument doc, DocumentFilterContext context)
    {
        doc.Info.Title = _settings.Title;
    }
}
