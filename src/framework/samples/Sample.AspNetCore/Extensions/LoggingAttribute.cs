using Microsoft.AspNetCore.Mvc.Filters;

namespace Sample.AspNetCore.Extensions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class EnableBodyRewind : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        var requestBodyString = await request.ReadBodyAsync();
        Console.WriteLine(requestBodyString);

        // Create a new memory stream to capture the response
        var originalBody = context.HttpContext.Response.Body;

        using var responseBody = new MemoryStream();
        context.HttpContext.Response.Body = responseBody;

        // Read the response body
        responseBody.Seek(0, SeekOrigin.Begin);
        string responseText = new StreamReader(responseBody).ReadToEnd();

        Console.WriteLine(responseText);

        // Copy the response body back to the original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBody);

        context.HttpContext.Response.Body = originalBody;
    }
}

public static class RequestExtensions
{
    public static async Task<string> ReadBodyAsync(this HttpRequest request)
    {
        // Ensure the request's body can be read multiple times 
        // (for the next middlewares in the pipeline).
        request.EnableBuffering();
        using var streamReader = new StreamReader(request.Body, leaveOpen: true);
        var requestBody = await streamReader.ReadToEndAsync();
        // Reset the request's body stream position for 
        // next middleware in the pipeline.
        request.Body.Position = 0;
        return requestBody;
    }
}
