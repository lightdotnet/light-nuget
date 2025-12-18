using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Light.AspNetCore.Modularity;

public abstract class AppModule : IModuleServiceCollection, IModuleBuilder, IModuleEndpoint
{
    public virtual void Add(IServiceCollection services)
    { }

    public virtual void Add(IServiceCollection services, IConfiguration configuration)
    { }

    public virtual void Use(IApplicationBuilder app)
    { }

    public virtual void Map(IEndpointRouteBuilder endpoints)
    { }
}