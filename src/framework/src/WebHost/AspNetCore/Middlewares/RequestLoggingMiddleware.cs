using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace Light.AspNetCore.Middlewares;

public class RequestLoggingMiddleware(
    RequestDelegate next,
    IOptions<RequestLoggingOptions> options,
    // must use Microsoft Logger because only Singleton services can be resolved by constructor injection in Middleware
    ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestLoggingOptions _settings = options.Value;
    private readonly IReadOnlyList<string> _excludePaths = BuildExcludePaths(options.Value);

    private static IReadOnlyList<string> BuildExcludePaths(RequestLoggingOptions settings)
    {
        var excludePath = new List<string> { "hangfire", "swagger" };
        if (settings.ExcludePaths is not null)
            excludePath.AddRange(settings.ExcludePaths);

        return excludePath;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (CheckSkipWriteLog(context.Request))
        {
            // Continue processing the request
            await next(context);
        }
        else
        {
            var timer = new Stopwatch();
            timer.Start();

            await WriteRequestBodyAsync(context);

            await WriteResponseAsync(context);

            timer.Stop();

            WriteRequestLog(context, timer.ElapsedMilliseconds);
        }
    }

    private bool CheckSkipWriteLog(HttpRequest httpRequest)
    {
        var path = httpRequest.Path.ToString();
        return _excludePaths.Any(c => path.Contains(c));
    }

    private void WriteRequestLog(HttpContext context, long elapsedMilliseconds)
    {
        var httpRequest = context.Request;

        var traceId = context.TraceIdentifier;
        var ip = httpRequest.HttpContext.Connection.RemoteIpAddress;
        var clientIp = ip == null ? "UnknownIP" : ip.ToString();

        var requestMethod = httpRequest.Method;
        var requestPath = httpRequest.Path;
        var requestQuery = httpRequest.QueryString.ToString();
        var requestScheme = httpRequest.Scheme;

        var statusCode = context.Response.StatusCode;

        var logContent = $"{traceId} {requestScheme} {requestMethod} {statusCode} {requestPath}{requestQuery} FromIP: {clientIp}";

        logger.LogInformation("{content} in {elapsedMilliseconds} ms", logContent, elapsedMilliseconds);
    }

    private async Task WriteRequestBodyAsync(HttpContext context)
    {
        if (_settings.IncludeRequest is false)
        {
            return;
        }

        var requestBody = await ReadBodyAsync(context.Request);

        if (string.IsNullOrEmpty(requestBody))
        {
            return;
        }

        var logContent = $"{context.TraceIdentifier} request {requestBody}";

        logger.LogInformation("{content}", logContent);
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        // Ensure the request's body can be read multiple times 
        // (for the next middlewares in the pipeline).
        request.EnableBuffering();
        using var streamReader = new StreamReader(request.Body, leaveOpen: true);
        var requestBody = await streamReader.ReadToEndAsync();
        // Reset the request's body stream position for 
        // next middleware in the pipeline.
        request.Body.Position = 0;

        // minify request if is JSON
        if (request.ContentType?.Contains("application/json") is true)
            requestBody = Minify(requestBody);

        return requestBody;
    }

    private static string Minify(string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            var obj = JsonSerializer.Deserialize<object>(json);
            json = JsonSerializer.Serialize(obj);
        }

        return json;
    }

    private async Task WriteResponseAsync(HttpContext context)
    {
        if (_settings.IncludeResponse is false)
        {
            await next(context);
            return;
        }

        // Create a new memory stream to capture the response
        var originalBody = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Continue processing the request; let exceptions propagate to the
            // exception-handling pipeline instead of being swallowed here
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        try
        {
            // Read the response body
            responseBody.Seek(0, SeekOrigin.Begin);
            string responseText = await new StreamReader(responseBody).ReadToEndAsync();

            if (!string.IsNullOrEmpty(responseText))
            {
                var logContent = $"{context.TraceIdentifier} response {responseText}";

                logger.LogInformation("{content}", logContent);
            }

            // Copy the response body back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBody);
        }
        catch (Exception ex)
        {
            logger.LogError("Unhandler exception when write request log with error {error}.", ex.Message);
        }
    }
}
