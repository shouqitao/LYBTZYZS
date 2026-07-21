using System.Security.Claims;
using LYBT.Infrastructure.Web;
using LYBT.LocalWebAPI.Commands;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private readonly ISender _sender;

    public AuthController(ISender sender, ILogger<AuthController> logger) : base(logger)
    {
        _sender = sender;
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LocalLogin")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new LocalLoginCommand(request), ct);
        if (!result.Success)
            return BusinessFail(result.Message ?? "登录失败");
        return Success(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        _logger.LogInformation("[AUTH] Local logout - UserName={UserName}", request?.UserName ?? "(unknown)");
        return Success("已登出");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new LocalRefreshTokenCommand(request), ct);
        if (!result.Success)
            return BusinessFail(result.Message ?? "刷新令牌失败");
        return Success(result);
    }

    [HttpPost("auto-login")]
    [AllowAnonymous]
    public async Task<IActionResult> AutoLogin([FromBody] AutoLoginRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new LocalAutoLoginCommand(request), ct);
        if (!result.Success)
            return BusinessFail(result.Message ?? "自动登录失败");
        return Success(result);
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken(CancellationToken ct)
    {
        var userId = GetCurrentUserId(User);
        var result = await _sender.Send(new LocalValidateTokenQuery(userId), ct);
        return Success(result);
    }
}
