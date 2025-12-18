using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Sample.AspNetCore.Swagger
{
    [ApiVersion("1")]
    [ApiExplorerSettings(GroupName = "G2")]
    public class Group2v1oController : VersionedApiController
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Group2_v1");
        }
    }
}
