using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Utilities.Security;
using System.Security.Claims;
using LYBT.Entities.Users;
using System.Threading.Tasks;
using System.Collections.Generic;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Local authentication controller: login, logout, refresh, validate, auto-login endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LocalWebApiDbContext _db;

    public AuthController(LocalWebApiDbContext db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && !u.IsDeleted);

        if (user == null)
        {
            return Unauthorized();
        }

        // Verify password using existing helper
        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash).IsSuccess)
        {
            return Unauthorized();
        }

        // Generate JWT
        var token = LocalJwtConfig.GenerateToken(user);

        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

    /// <summary>
    /// 登出 — 清除本地会话（本地模式下 token 无状态，返回成功即可）
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        // Local JWT is stateless; client should discard the token.
        return Ok(new { Success = true, Message = "已登出" });
    }

    /// <summary>
    /// 刷新 JWT token — 本地模式下用当前 token 换取新 token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Extract user id from current token claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Token 无效或已过期" });
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            return Unauthorized(new { Message = "用户不存在" });
        }

        var newToken = LocalJwtConfig.GenerateToken(user);

        return Ok(new
        {
            Token = newToken,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

    /// <summary>
    /// 验证当前 token 有效性
    /// </summary>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Ok(new { IsValid = false, Message = "Token 无效" });
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            return Ok(new { IsValid = false, Message = "用户不存在或已禁用" });
        }

        return Ok(new
        {
            IsValid = true,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

    /// <summary>
    /// 自动登录 — 使用已保存的凭据（本地模式下直接验证用户存在性）
    /// </summary>
    [HttpPost("auto-login")]
    public async Task<IActionResult> AutoLogin([FromBody] AutoLoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && !u.IsDeleted);

        if (user == null)
        {
            return Unauthorized();
        }

        var token = LocalJwtConfig.GenerateToken(user);

        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

}
