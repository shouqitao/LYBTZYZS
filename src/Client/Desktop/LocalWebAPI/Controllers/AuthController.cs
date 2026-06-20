using LYBT.Entities.Users;
using LYBT.Infrastructure.Web;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger) : base(logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new { Message = "用户名或密码错误" });

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            return Unauthorized(new { Message = "用户名或密码错误" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
            return Unauthorized(new { Message = "用户名或密码错误" });

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var role = ParseUserRole(roles);

        var token = LocalJwtConfig.GenerateToken(user, roles);

        _logger.LogInformation("[AUTH] Local login succeeded - UserName={UserName} Role={Role}",
            request.UserName, role);

        var response = new LoginResponse
        {
            Token = token,
            User = new UserDetailDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                RealName = user.RealName,
                Role = role,
                Status = CommonStatus.Enabled,
                PhoneNumber = user.PhoneNumber,
            },
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Message = "登录成功",
            Data = response
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        _logger.LogInformation("[AUTH] Local logout - UserName={UserName}", request?.UserName ?? "(unknown)");
        return Ok(new { Success = true, Message = "已登出" });
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken()
    {
        var userId = GetCurrentUserId(User);
        if (userId == Guid.Empty)
            return Ok(new ApiResponse<ValidateTokenResponse>
            {
                Success = false,
                Message = "Token 无效"
            });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Ok(new ApiResponse<ValidateTokenResponse>
            {
                Success = false,
                Message = "用户不存在"
            });

        var roles = await _userManager.GetRolesAsync(user);
        var role = ParseUserRole(roles);

        return Ok(new ApiResponse<ValidateTokenResponse>
        {
            Success = true,
            Message = "Token 验证成功",
            Data = new ValidateTokenResponse
            {
                IsValid = true,
                UserId = (int)role,
                Username = user.UserName,
                Role = role.ToString(),
            }
        });
    }

    private static UserRole ParseUserRole(IList<string> roles)
    {
        if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], ignoreCase: true, out var role))
            return role;

        return UserRole.Doctor;
    }
}
