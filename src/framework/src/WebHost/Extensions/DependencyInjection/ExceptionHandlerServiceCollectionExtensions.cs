using Light.AspNetCore.ExceptionHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Extensions.DependencyInjection;

public static class ExceptionHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<ExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
