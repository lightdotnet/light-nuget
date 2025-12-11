using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Sample.AspNetCore.Swagger
{
    [ApiVersion("1")]
    [ApiExplorerSettings(GroupName = "G1")]
    public class Group1v1oController : VersionedApiController
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Group1_v1");
        }
    }
}
