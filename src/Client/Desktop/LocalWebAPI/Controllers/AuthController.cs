using LYBT.Entities.Users;
using LYBT.Infrastructure.Web;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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

        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            Username = user.UserName,
            Role = role
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
            return Ok(new { IsValid = false, Message = "Token 无效" });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Ok(new { IsValid = false, Message = "用户不存在" });

        var roles = await _userManager.GetRolesAsync(user);
        var role = ParseUserRole(roles);

        return Ok(new
        {
            IsValid = true,
            UserId = user.Id,
            Username = user.UserName,
            Role = role
        });
    }

    private static UserRole ParseUserRole(IList<string> roles)
    {
        if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], ignoreCase: true, out var role))
            return role;

        return UserRole.Doctor;
    }
}
