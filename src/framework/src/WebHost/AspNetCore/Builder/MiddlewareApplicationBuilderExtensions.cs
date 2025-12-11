using Light.AspNetCore.ExceptionHandlers;
using Light.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.AspNetCore.Builder;

public static class MiddlewareApplicationBuilderExtensions
{
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

    public static IApplicationBuilder UseUlidTraceId(this IApplicationBuilder app)
    {
        app.UseMiddleware<UlidTraceIdMiddleware>();

        return app;
    }

    //[Obsolete("please use AddGlobalExceptionHandler() instead")]
    public static IApplicationBuilder UseLightExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();

        return app;
    }
}
