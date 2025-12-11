using Light.Contracts;
using Light.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace Light.AspNetCore.ExceptionHandlers;

internal static class ExceptionHandlerExtensions
{
    public static async Task HandleExceptionAsync(
        this HttpContext httpContext,
        Exception exception,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // exclude trace exception from Hangfire
        var isHangfireException = IsHangfireException(httpContext, exception);
        if (isHangfireException)
            return;

        var traceId = httpContext.TraceIdentifier;
        var response = httpContext.Response;

        if (exception is not ExceptionBase && exception.InnerException != null)
        {
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }
        }

        string message = exception.Message.Trim();

        var settings = httpContext.RequestServices.GetRequiredService<IOptions<ExceptionHandlerOptions>>().Value;

        switch (exception)
        {
            case ValidationException e:
                {
                    response.StatusCode = (int)e.StatusCode;

                    var errors = e.ValidationErrors
                        .Select(s =>
                        {
                            // convert error from dictionary to model_prop: error1,error2,...
                            var modelState = $"{s.Key}: {string.Join(",", s.Value)}";

                            return modelState;
                        });

                    if (errors.Any())
                    {
                        message = string.Join("|", errors);
                    }

                    break;
                }
            case ExceptionBase e:
                response.StatusCode = (int)e.StatusCode;
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                message = $"Not Found";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                message = settings.HideUndentifyException ? $"Internal Server Error" : message;
                break;
        }

        // Write exception log with Trace ID
        var exceptionSource = exception.TargetSite?.DeclaringType?.FullName;

        var errorModel = new
        {
            source = exceptionSource,
            exception = exception.Message,
        };

        var errorContent = $"{traceId} error {response.StatusCode}";
        logger.LogError("{errorContent} {@errorModel}", errorContent, errorModel);

        // Write exception as Result
        if (!response.HasStarted)
        {
            var result = new Result
            {
                Code = response.StatusCode.ToString(),
                Message = message,
                RequestId = traceId,
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            response.ContentType = MediaTypeNames.Application.Json;

            await response.WriteAsJsonAsync(result, jsonOptions, cancellationToken: cancellationToken);
        }
        else
        {
            logger.LogError("Can't write error response. Response has already started.");
        }
    }

    private static bool IsHangfireException(HttpContext httpContext, Exception exception)
    {
        // Not write exception from Hangfire
        bool isHangfire = httpContext.Request.Path.ToString().Contains("hangfire");

        var isHangfireLoginError = exception.Message.Contains("StatusCode cannot be set because the response has already started.");

        return isHangfire && isHangfireLoginError;
    }
}
