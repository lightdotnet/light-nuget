namespace Sample.AspNetCore.Modules;

public class OrderModule : Light.AspNetCore.Modularity.AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<OrderMiddleware>();
        services.AddSingleton<OrderModuleService>();

        //Serilog.Log.Warning("Module {name} injected", GetType().FullName);
    }

    public override void Use(IApplicationBuilder builder)
    {
        builder.UseMiddleware<OrderMiddleware>();

        //Serilog.Log.Warning("Module {name} injected", GetType().FullName);
    }
}

public class ProductModule : Light.AspNetCore.Modularity.AppModule
{
    public override void Add(IServiceCollection services)
    {
        services.AddSingleton<ProductMiddleware>();
        services.AddTransient<ProductModuleService>();

        //Serilog.Log.Warning("Module Product service injected");
    }

    public override void Use(IApplicationBuilder builder)
    {
        builder.UseMiddleware<ProductMiddleware>();
    }
}

