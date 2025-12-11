namespace Sample.AspNetCore.Modules;

public class ProductMiddleware(ILogger<ProductMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        logger.LogInformation("Product middleware");
        await next(context);
    }
}
