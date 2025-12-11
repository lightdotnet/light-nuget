using Light.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sample.AspNetCore.Services;

namespace Sample.AspNetCore.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestController(
        IMemoryCache memoryCache,
        IDateTimeService dateTimeService) : ControllerBase
    {
        private readonly string _cacheKey = "Key";

        private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

        [HttpGet("mem")]
        public async Task<IActionResult> GetAsync()
        {
            var memObj = await memoryCache.GetOrCreateAsync(_cacheKey,
                async factory =>
                {
                    var r = 1;
                    await Task.Yield();
                    return r;
                });

            return Ok(memObj);
        }

        [HttpGet("dis")]
        public IActionResult GetDisAsync()
        {
            return Ok(_now);
        }

        [HttpGet("throw")]
        public IActionResult ThrowAsync()
        {
            Throw();

            return Ok();
        }

        [HttpGet("now")]
        public IActionResult GetNowAsync()
        {
            return Ok(dateTimeService.Now);
        }

        [HttpGet("trace_id")]
        public IActionResult TraceId()
        {
            var res = HttpContext.TraceIdentifier;
            return Ok(res);
        }

        private static void Throw()
        {
            Console.WriteLine("...");

            throw new InternalServerErrorException("Server error");
        }
    }
}
