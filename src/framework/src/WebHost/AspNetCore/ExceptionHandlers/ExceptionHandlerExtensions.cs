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
    /// <summary>
    /// Non-standard status code (nginx convention) used when the client closed the connection.
    /// </summary>
    internal const int ClientClosedRequestStatusCode = 499;

    private static readonly JsonSerializerOptions ErrorResponseJsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Handles the exception and writes an error <see cref="Result"/> to the response.
    /// </summary>
    /// <returns>
    /// <c>false</c> when the exception could not be handled because the response has already started;
    /// the caller should rethrow so the server aborts the connection.
    /// </returns>
    public static async Task<bool> HandleExceptionAsync(
        this HttpContext httpContext,
        Exception exception,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // exclude trace exception from Hangfire
        var isHangfireException = IsHangfireException(httpContext, exception);
        if (isHangfireException)
            return true;

        var traceId = httpContext.TraceIdentifier;
        var response = httpContext.Response;

        // client disconnected: not a server error, nothing to write
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("{traceId} request aborted by client", traceId);

            if (!response.HasStarted)
                response.StatusCode = ClientClosedRequestStatusCode;

            return true;
        }

        // headers are already sent: can't change status code or write an error body
        if (response.HasStarted)
        {
            logger.LogError(exception, "{traceId} can't write error response. Response has already started.", traceId);
            return false;
        }

        exception = Unwrap(exception);

        string message = exception.Message.Trim();

        var settings = httpContext.RequestServices.GetRequiredService<IOptions<ExceptionHandlerOptions>>().Value;

        switch (exception)
        {
            case ValidationException e:
                {
                    response.StatusCode = (int)e.StatusCode;

                    if (e.ValidationErrors.Count > 0)
                    {
                        var errors = e.ValidationErrors
                            .Select(s =>
                            {
                                // convert error from dictionary to model_prop: error1,error2,...
                                var modelState = $"{s.Key}: {string.Join(",", s.Value)}";

                                return modelState;
                            });

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
                message = settings.HideUnidentifiedException ? $"Internal Server Error" : message;
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
        logger.LogError(exception, "{errorContent} {@errorModel}", errorContent, errorModel);

        // Write exception as Result
        var result = new Result
        {
            Code = response.StatusCode.ToString(),
            Message = message,
            RequestId = traceId,
        };

        response.ContentType = MediaTypeNames.Application.Json;

        await response.WriteAsJsonAsync(result, ErrorResponseJsonOptions, cancellationToken: cancellationToken);

        return true;
    }

    /// <summary>
    /// Returns the first <see cref="ExceptionBase"/> in the inner-exception chain,
    /// or the innermost exception when there is none.
    /// </summary>
    internal static Exception Unwrap(Exception exception)
    {
        var current = exception;
        while (current is not ExceptionBase && current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }

    private static bool IsHangfireException(HttpContext httpContext, Exception exception)
    {
        // Not write exception from Hangfire
        bool isHangfire = httpContext.Request.Path.ToString().Contains("hangfire");

        var isHangfireLoginError = exception.Message.Contains("StatusCode cannot be set because the response has already started.");

        return isHangfire && isHangfireLoginError;
    }
}
