using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Light.AspNetCore.ExceptionHandlers;

public class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // false when the response has already started: let the framework handle/rethrow it
        return await httpContext.HandleExceptionAsync(exception, logger, cancellationToken);
    }
}
