using Light.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogger(this IServiceCollection services)
    {
        return services.AddScoped(typeof(ILogger<>), typeof(LoggerAdapter<>));
    }
}
