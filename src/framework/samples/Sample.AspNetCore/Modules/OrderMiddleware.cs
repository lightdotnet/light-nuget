namespace Sample.AspNetCore.Modules;

public class OrderMiddleware(ILogger<OrderMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        logger.LogInformation("Order middleware");
        await next(context);
    }
}
