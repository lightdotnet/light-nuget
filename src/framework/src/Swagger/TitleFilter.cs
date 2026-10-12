using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Light.AspNetCore.Swagger;

public class TitleFilter(IOptions<SwaggerOptions> options) : IDocumentFilter
{
    private readonly SwaggerOptions _settings = options.Value;

    public void Apply(OpenApiDocument doc, DocumentFilterContext context)
    {
        // keep the document's own title (e.g. per API version) when no custom title is configured
        if (!string.IsNullOrEmpty(_settings.Title))
            doc.Info.Title = _settings.Title;
    }
}
