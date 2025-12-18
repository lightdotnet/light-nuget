using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Light.AspNetCore.Mvc;

/// <summary>
/// Abstract BaseApi Controller Class
/// </summary>

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    /// <summary>
    /// Default success response
    /// </summary>
    /// <returns></returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    public new virtual IActionResult Ok()
    {
        var result = Result.Success();
        result.RequestId = HttpContext.TraceIdentifier;

        return result.ToActionResult();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public virtual IActionResult Ok<T>(T data)
    {
        var result = data as IResult ?? Result<T>.Success(data);
        result.RequestId = HttpContext.TraceIdentifier;
        return result.ToActionResult();
    }
}
