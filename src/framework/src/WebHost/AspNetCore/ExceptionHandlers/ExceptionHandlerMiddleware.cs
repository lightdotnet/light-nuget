using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Light.AspNetCore.ExceptionHandlers;

// must use Microsoft Logger because only Singleton services can be resolved by constructor injection in Middleware
// Not inherit from IMiddleware for don't need register Middleware as a service
public class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await context.HandleExceptionAsync(ex, logger);
        }
    }
}
