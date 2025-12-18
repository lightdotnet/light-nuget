namespace Sample.AspNetCore.Extensions;

public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        // Capture the request body
        context.Request.EnableBuffering();
        var requestBodyStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(requestBodyStream);
        requestBodyStream.Seek(0, SeekOrigin.Begin);
        string requestBodyText = new StreamReader(requestBodyStream).ReadToEnd();

        // Capture the response body
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Call the next middleware in the pipeline
        await next(context);

        // Log the response
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        string responseBodyText = await new StreamReader(responseBodyStream).ReadToEndAsync();

        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var responseStatusCode = context.Response.StatusCode;

        logger.LogInformation("{method} {statusCode} {path}\r\n{request}\r\n{response}",
            requestMethod, responseStatusCode, requestPath,
            requestBodyText,
            responseBodyText);

        // Copy the response body back to the original stream
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalResponseBodyStream);
    }
}
