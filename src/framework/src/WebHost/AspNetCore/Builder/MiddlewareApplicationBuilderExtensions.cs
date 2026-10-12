using Light.AspNetCore.ExceptionHandlers;
using Light.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.AspNetCore.Builder;

public static class MiddlewareApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="RequestLoggingMiddleware"/> when <see cref="RequestLoggingOptions.Enable"/> is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Register the exception handler (<see cref="UseLightExceptionHandler"/> or <c>UseExceptionHandler()</c>)
    /// <b>before</b> this call, so exceptions propagate out of the request logging middleware. If an exception handler
    /// runs inside it instead (registered after this call) and the failed endpoint had written to
    /// <c>Response.BodyWriter</c> without flushing, those stale bytes are still buffered in the capturing writer and
    /// get flushed together with the error body.
    /// </remarks>
    public static IApplicationBuilder UseLightRequestLogging(this IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<IOptions<RequestLoggingOptions>>().Value;

        if (settings.Enable)
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
        }

        return app;
    }

    public static IApplicationBuilder UseGuidTraceId(this IApplicationBuilder app)
    {
        app.UseMiddleware<GuidTraceIdMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseGuidV7TraceId(this IApplicationBuilder app)
    {
        app.UseMiddleware<GuidV7TraceIdMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseLightExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();

        return app;
    }
}
