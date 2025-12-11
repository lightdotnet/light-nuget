using Microsoft.Extensions.Options;

namespace Sample.AspNetCore.TestOption;

public static class Startup
{
    public static IServiceCollection AddTestOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Overide by action
        services.Add();
        services.AddByAction();

        // Overide by IConfigureOptions
        services.AddByIConfigureOptions();

        // Overide by read section values
        services.AddByReadSection(configuration);

        services.AddTransient<IConfigureOptions<Light.AspNetCore.ExceptionHandlers.ExceptionHandlerOptions>, ErrorHandlerOptions>();

        return services;
    }

    public static IServiceCollection Add(this IServiceCollection services)
    {
        services.Configure(new Action<TestOptions>(x =>
        {
            x.Name = "test action";
            x.Description = "test action";
        }));

        return services;
    }

    public static IServiceCollection AddByAction(this IServiceCollection services)
    {
        // Overide by action
        services.ConfigureByAction(opt =>
        {
            opt.Name = "Test from Action";
            opt.Description = "Test description from Action";
        });
        return services;
    }

    public static IServiceCollection ConfigureByAction(this IServiceCollection services, Action<TestOptions> action)
    {
        // this will overide action manual set to default options
        services.Configure(action);

        return services;
    }

    public static IServiceCollection AddByIConfigureOptions(this IServiceCollection services)
    {
        // Overide by IConfigureOptions
        services.AddTransient<IConfigureOptions<TestOptions>, CustomTestOptions>();

        return services;
    }

    public static IServiceCollection AddByReadSection(this IServiceCollection services, IConfiguration configuration)
    {
        // Overide by read appsettings        
        services.Configure<TestOptions>(configuration.GetSection("Test"));

        // Overide by BindConfiguration
        services.AddOptions<TestOptions>().BindConfiguration("Test1");

        return services;
    }
}