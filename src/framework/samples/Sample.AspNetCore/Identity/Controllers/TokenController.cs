using Light.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Sample.AspNetCore.Identity;

namespace Sample.AspNetCore.Identity.Controllers;

public class TokenController(
    JwtTokenMananger tokenService,
    IOptions<JwtOptions> options) : VersionedApiController
{
    private readonly JwtOptions _jwt = options.Value;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await Task.Yield(); // Simulate async operation

        return Ok(tokenService.TimeNow);
    }

    [HttpPost("token")]
    public async Task<IActionResult> GetTokenAsync(string userName)
    {
        var user = await tokenService.UserManager.FindByNameAsync(userName);

        return Ok(await tokenService.GenerateTokenByAsync(
            user!,
            _jwt.Issuer,
            _jwt.SecretKey,
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(7)));
    }

    [HttpPost("token/refresh")]
    public async Task<IActionResult> RefreshTokenAsync(string userName, string refreshToken)
    {
        var user = await tokenService.UserManager.FindByNameAsync(userName);

        return Ok(await tokenService.RefreshTokenAsync(
            user!,
            refreshToken,
            _jwt.Issuer,
            _jwt.SecretKey,
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(7)));
    }

    [HttpPost("token/revoke")]
    public async Task<IActionResult> RevokeAsync(string userId, string tokenId)
    {
        await tokenService.RevokedAsync(userId, tokenId);
        return Ok();
    }

    [HttpGet("token/list/{userId}")]
    public async Task<IActionResult> GetListAsync(string userId)
    {
        var res = await tokenService.GetUserTokensAsync(userId);
        return Ok(res);
    }

    [HttpGet("check_token")]
    public async Task<IActionResult> CheckToken()
    {
        var accessToken = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        var res = await tokenService.IsTokenValidAsync(accessToken);
        return Ok(res);
    }
}