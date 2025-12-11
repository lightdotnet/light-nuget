using Light.Serilog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sample.AspNetCore.TestOption;

namespace Sample.AspNetCore.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OptionsController(
        IConfiguration configuration,
        IOptions<TestOptions> options) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var config = SerilogOptionsExtensions.GetWriteToOptions(configuration);
            return Ok(config);
        }

        [HttpGet("test")]
        public IActionResult GetTest()
        {
            var res = options.Value;
            return Ok(res);
        }
    }
}
