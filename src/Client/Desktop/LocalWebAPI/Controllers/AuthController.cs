using LYBT.Infrastructure.Web;
using LYBT.LocalWebAPI.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using System.Security.Claims;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Local authentication controller: uses IAuthService for credential verification
/// + LocalJwtConfig for simplified local JWT generation (1-year token, no refresh).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IAutoLoginService _autoLoginService;
    private readonly AppDbContext _db;

    public AuthController(
        IAuthService authService,
        IAutoLoginService autoLoginService,
        AppDbContext db,
        ILogger<AuthController> logger) : base(logger)
    {
        _authService = authService;
        _autoLoginService = autoLoginService;
        _db = db;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized();

        // Use Service layer for credential verification (password, status, lockout)
        var verifyResult = await _authService.VerifyCredentialsAsync(request);
        if (!verifyResult.IsSuccess)
            return Unauthorized(new { Message = verifyResult.ErrorMessage });

        // Load user for local JWT generation
        var userId = Guid.Parse(verifyResult.Data!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Unauthorized();

        // Generate local JWT (simplified: 1-year, no refresh token)
        var token = LocalJwtConfig.GenerateToken(user);
        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        return Ok(new { Success = true, Message = "已登出" });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { Message = "Token 无效或已过期" });

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            return Unauthorized(new { Message = "用户不存在" });

        // Check user status
        if (user.Status != LYBT.Shared.Models.Enums.CommonStatus.Enabled)
            return Unauthorized(new { Message = "账户已被禁用" });

        var newToken = LocalJwtConfig.GenerateToken(user);
        return Ok(new
        {
            Token = newToken,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Ok(new { IsValid = false, Message = "Token 无效" });

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            return Ok(new { IsValid = false, Message = "用户不存在" });

        if (user.Status != LYBT.Shared.Models.Enums.CommonStatus.Enabled)
            return Ok(new { IsValid = false, Message = "账户已被禁用" });

        return Ok(new
        {
            IsValid = true,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

    [HttpPost("auto-login")]
    public async Task<IActionResult> AutoLogin([FromBody] AutoLoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName)
            || string.IsNullOrWhiteSpace(request.AutoLoginToken))
            return Unauthorized();

        // Use Service layer for auto-login (credential verification + status check)
        var loginResult = await _authService.LoginWithAutoTokenAsync(request);
        if (!loginResult.IsSuccess || loginResult.Data == null)
            return Unauthorized(new { Message = loginResult.ErrorMessage ?? "自动登录失败" });

        // Replace remote JWT with local simplified JWT
        var loginData = loginResult.Data;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == loginData.User.Id);
        if (user == null)
            return Unauthorized();

        var localToken = LocalJwtConfig.GenerateToken(user);
        return Ok(new
        {
            Token = localToken,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }
}
