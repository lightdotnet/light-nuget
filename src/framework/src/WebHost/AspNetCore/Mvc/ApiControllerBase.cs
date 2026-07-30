using Light.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Light.AspNetCore.Mvc;

/// <summary>
/// Abstract BaseApi Controller Class
/// </summary>

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Default success response, wrapped in the framework's Result envelope
    /// </summary>
    /// <returns></returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    public new virtual IActionResult Ok()
    {
        var result = Result.Success();
        result.RequestId = HttpContext.TraceIdentifier;

        return result.ToActionResult();
    }

    /// <summary>
    /// Default success response with data, wrapped in the framework's Result envelope
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public virtual IActionResult Ok<T>(T data)
    {
        var result = data as ResultBase ?? Result<T>.Success(data);
        result.RequestId = HttpContext.TraceIdentifier;
        return result.ToActionResult();
    }
}
