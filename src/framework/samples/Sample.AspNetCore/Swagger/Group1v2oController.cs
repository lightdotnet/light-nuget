using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Sample.AspNetCore.Swagger
{
    [ApiVersion("2")]
    [ApiExplorerSettings(GroupName = "g1")]
    public class Group1v2oController : VersionedApiController
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Group2_v2");
        }
    }
}
