using Light.Domain;
using Microsoft.AspNetCore.Http;

namespace Light.AspNetCore.Middlewares;

public class GuidV7TraceIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        context.TraceIdentifier = LightId.NewId();
        string id = context.TraceIdentifier;
        context.Response.Headers["X-Trace-Id"] = id;
        await next(context);
    }
}
