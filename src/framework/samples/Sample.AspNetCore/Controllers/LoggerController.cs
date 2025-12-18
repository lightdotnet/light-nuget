using Microsoft.AspNetCore.Mvc;

namespace Sample.AspNetCore.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoggerController(ILogger<LoggerController> logger) : ControllerBase
    {
        [HttpGet]
        public IActionResult Export()
        {
            var id = Guid.NewGuid().ToString();

            logger.LogInformation("Write information log {id}", id);

            logger.LogWarning("Write warning log {id}", id);

            logger.LogError("Write error log {id}", id);

            return Ok();
        }
    }
}