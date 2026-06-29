using System.IdentityModel.Tokens.Jwt;
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
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    private readonly IConfiguration _configuration;
    
    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger) : base(logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(ApiResponse<object>.CreateFail("用户名或密码错误", new { code = "AuthInvalidCredentials" }));

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            return Unauthorized(ApiResponse<object>.CreateFail("用户名或密码错误", new { code = "AuthInvalidCredentials" }));

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<object>.CreateFail("用户名或密码错误", new { code = "AuthInvalidCredentials" }));

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
        return Ok(new ApiResponse { Success = true, Message = "已登出" });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(ApiResponse<object>.CreateFail("令牌不能为空", new { code = "AuthTokenInvalid" }));

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(request.RefreshToken);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<object>.CreateFail("无效的令牌", new { code = "AuthTokenInvalid" }));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(ApiResponse<object>.CreateFail("用户不存在", new { code = "AuthTokenInvalid" }));

            var roles = await _userManager.GetRolesAsync(user);
            var token = LocalJwtConfig.GenerateToken(user, roles);
            var role = ParseUserRole(roles);

            _logger.LogInformation("[AUTH] Local token refresh - UserName={UserName}", user.UserName);

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
                Message = "Token刷新成功",
                Data = response
            });
        }
        catch (Exception)
        {
            return Unauthorized(ApiResponse<object>.CreateFail("无效的令牌", new { code = "AuthTokenInvalid" }));
        }
    }

    [HttpPost("auto-login")]
    [AllowAnonymous]
    public async Task<IActionResult> AutoLogin([FromBody] AutoLoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AutoLoginToken))
            return Unauthorized(ApiResponse<object>.CreateFail("自动登录令牌不能为空", new { code = "AuthTokenInvalid" }));

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["LocalJwt:SecretKey"] ?? "LYBT-LocalWebAPI-Secret-Key-2024-DoNotUseInProduction";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(request.AutoLoginToken, validationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtToken)
                return Unauthorized(ApiResponse<object>.CreateFail("无效的自动登录令牌格式", new { code = "AuthTokenInvalid" }));

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<object>.CreateFail("自动登录令牌缺少用户信息", new { code = "AuthTokenInvalid" }));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(ApiResponse<object>.CreateFail("用户不存在", new { code = "AuthTokenInvalid" }));

            var roles = await _userManager.GetRolesAsync(user);
            var token = LocalJwtConfig.GenerateToken(user, roles);
            var role = ParseUserRole(roles);

            _logger.LogInformation("[AUTH] Local auto-login - UserName={UserName} Role={Role}",
                user.UserName, role);

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
                Message = "自动登录成功",
                Data = response
            });
        }
        catch (Exception)
        {
            return Unauthorized(ApiResponse<object>.CreateFail("自动登录令牌无效", new { code = "AuthTokenInvalid" }));
        }
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
