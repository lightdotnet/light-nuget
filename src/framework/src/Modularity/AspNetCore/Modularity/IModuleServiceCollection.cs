using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Light.AspNetCore.Modularity;

public interface IModuleServiceCollection
{
    /// <summary>
    /// Module Service Collection
    /// </summary>
    void Add(IServiceCollection services);

    /// <summary>
    /// Module Service Collection with IConfiguration
    /// </summary>
    void Add(IServiceCollection services, IConfiguration configuration);
}