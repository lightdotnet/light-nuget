using Light.Identity.EntityFrameworkCore;
using Light.Identity.Extensions;
using Light.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Sample.AspNetCore.Identity.Controllers;

public class TokenController(
    AppIdentityDbContext context,
    UserManager<User> userManager,
    IOptions<JwtOptions> options) : VersionedApiController
{
    private readonly JwtOptions _jwt = options.Value;

    [HttpPost("token")]
    public async Task<IActionResult> GetTokenAsync(string userName)
    {
        var user = await userManager.FindByNameAsync(userName);
        var userClaims = await context.QueryUserClaims(user!.Id).ToListAsync();

        var token = JwtHelper.GenerateToken(
            _jwt.Issuer,
            userClaims.Select(s => new System.Security.Claims.Claim(s.Type, s.Value)),
            DateTime.Today.AddDays(7),
            _jwt.SecretKey);

        return Ok(token);
    }
}