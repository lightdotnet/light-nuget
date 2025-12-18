using Microsoft.AspNetCore.Http;

namespace Light.AspNetCore.Middlewares;

public class GuidTraceIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        context.TraceIdentifier = Guid.NewGuid().ToString();
        string id = context.TraceIdentifier;
        context.Response.Headers["X-Trace-Id"] = id;
        await next(context);
    }
}