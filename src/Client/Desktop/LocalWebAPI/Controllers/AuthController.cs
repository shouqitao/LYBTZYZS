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
    private readonly AppDbContext _db;

    public AuthController(
        IAuthService authService,
        AppDbContext db,
        ILogger<AuthController> logger) : base(logger)
    {
        _authService = authService;
        _db = db;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private static readonly Dictionary<string, (int Count, DateTime ResetAt)> _loginAttempts = new();
    private static readonly object _rateLock = new();
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(1);

    private bool CheckRateLimit(string userName)
    {
        lock (_rateLock)
        {
            if (_loginAttempts.TryGetValue(userName, out var entry))
            {
                if (DateTime.UtcNow < entry.ResetAt)
                {
                    if (entry.Count >= MaxAttempts)
                        return false;
                    _loginAttempts[userName] = (entry.Count + 1, entry.ResetAt);
                }
                else
                {
                    _loginAttempts[userName] = (1, DateTime.UtcNow.Add(RateWindow));
                }
            }
            else
            {
                _loginAttempts[userName] = (1, DateTime.UtcNow.Add(RateWindow));
            }
            return true;
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized();

        if (!CheckRateLimit(request.UserName))
            return StatusCode(429, new { Message = "登录尝试过于频繁，请稍后再试" });

        var verifyResult = await _authService.VerifyCredentialsAsync(request);
        if (!verifyResult.IsSuccess)
            return Unauthorized(new { Message = verifyResult.ErrorMessage });

        var userId = Guid.Parse(verifyResult.Data!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Unauthorized();

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
}
