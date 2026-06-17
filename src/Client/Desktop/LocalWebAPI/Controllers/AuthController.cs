using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Web;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db,
        ILogger<AuthController> logger) : base(logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized();

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            return Unauthorized(new { Message = "用户名或密码错误" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
            return Unauthorized(new { Message = "用户名或密码错误" });

        var businessUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        if (businessUser == null)
            return Unauthorized();

        var token = LocalJwtConfig.GenerateToken(businessUser);
        return Ok(new
        {
            Token = token,
            UserId = businessUser.Id,
            Username = businessUser.UserName,
            Role = businessUser.Role
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        return Ok(new { Success = true, Message = "已登出" });
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

        if (user.Status != CommonStatus.Enabled)
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
