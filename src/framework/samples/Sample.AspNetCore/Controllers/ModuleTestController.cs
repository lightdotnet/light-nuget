using Light.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sample.AspNetCore.Modules;

namespace Sample.AspNetCore.Controllers;

[Route("[controller]")]
[ApiController]
public class ModuleTestController(
    OrderModuleService orderModuleService,
    ProductModuleService productModuleService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var obj = new
        {
            productModuleService.Products,
            ProductId = productModuleService.GetProductId,
            OrderId = orderModuleService.GetOrderId
        };

        return Ok(obj);
    }

    [HttpPost]
    public IActionResult Post(int productId)
    {
        productModuleService.AddProduct(productId);
        return Ok(productModuleService.Products);
    }

    [HttpGet("test_result")]
    public IActionResult GetTest()
    {
        var res = new PagedResult<int>([1, 2, 3], 1, 20, 3);
        return Ok(res);
    }
}
