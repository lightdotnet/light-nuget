using Microsoft.AspNetCore.Builder;

namespace Light.AspNetCore.Modularity;

public interface IModuleBuilder
{
    /// <summary>
    /// Use Module Application Builder
    /// </summary>
    void Use(IApplicationBuilder app);
}