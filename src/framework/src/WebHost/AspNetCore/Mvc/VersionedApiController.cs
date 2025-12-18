using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Light.AspNetCore.Mvc;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public abstract class VersionedApiController : ApiControllerBase;