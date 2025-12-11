using Light.Extensions.Caching;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CachingController(ICacheService cacheService) : ControllerBase
    {
        private const string TEST_KEY = "test_key";

        [HttpGet]
        public async Task<IActionResult> Load()
        {
            Dictionary<string, decimal> testData = [];

            for (int i = 1; i < 200000; i++)
            {
                testData.Add($"W0001_{i}", i);
            }

            await cacheService.SetAsync(TEST_KEY, testData);

            return Ok();
        }

        [HttpGet("read")]
        public async Task<IActionResult> Get()
        {
            var res = await cacheService.GetAsync<Dictionary<string, decimal>>(TEST_KEY);
            var result = res["W0001_10000"];

            return Ok(result);
        }
    }
}