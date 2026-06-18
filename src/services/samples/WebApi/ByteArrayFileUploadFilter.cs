using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace WebApi;

public class FlexibleByteArrayModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var request = bindingContext.HttpContext.Request;

        // From Swagger file picker → multipart/form-data
        if (request.HasFormContentType)
        {
            var file = request.Form.Files.FirstOrDefault();
            if (file != null)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                bindingContext.Result = ModelBindingResult.Success(ms.ToArray());
                return;
            }
        }

        // From HttpClient PostAsJsonAsync → application/json base64
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var base64 = body.Trim('"'); // strip JSON quotes
        bindingContext.Result = ModelBindingResult.Success(Convert.FromBase64String(base64));
    }
}


public class ByteArrayModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(byte[]))
            return new BinderTypeModelBinder(typeof(FlexibleByteArrayModelBinder));
        return null;
    }
}

public class RawByteArrayBodyFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasRawByteArray = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(byte[])
                   && p.GetCustomAttribute<FromBodyAttribute>() != null);

        if (!hasRawByteArray) return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "binary"
                            }
                        }
                    }
                }
            }
        };
    }
}