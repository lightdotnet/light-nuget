using Light.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Sample.AspNetCore.Identity.Controllers;

public class RoleController(IRoleService _roleService) : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var res = await _roleService.GetAllAsync();
        return Ok(res);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        var res = await _roleService.GetByIdAsync(id);
        return Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRoleRequest request)
    {
        var res = await _roleService.CreateAsync(request);
        return Ok(res);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] RoleDto request)
    {
        var res = await _roleService.UpdateAsync(request);
        return Ok(res);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        var res = await _roleService.DeleteAsync(id);
        return Ok(res);
    }
}