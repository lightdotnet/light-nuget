using Microsoft.AspNetCore.Routing;

namespace Light.AspNetCore.Modularity;

public interface IModuleEndpoint
{
    /// <summary>
    /// Map Module Endpoint Route Builder
    /// </summary>
    void Map(IEndpointRouteBuilder endpoints);
}