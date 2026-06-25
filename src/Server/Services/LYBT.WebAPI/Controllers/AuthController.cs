using Asp.Versioning;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AuthController : BaseApiController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            ILogger<AuthController> logger)
            : base(logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("Login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            if (ValidateModel() is { } modelError) return modelError;

            if (request == null)
                return ValidationFail("登录请求不能为空");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return ValidationFail("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.Password))
                return ValidationFail("密码不能为空");

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
                return HandleAuthResult(Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "用户名或密码错误"), "登录失败");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
            if (!result.Succeeded)
                return HandleAuthResult(Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "用户名或密码错误"), "登录失败");

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var role = ParseUserRole(roles);
            string userType = role == UserRole.SuperAdmin ? RoleConstants.SuperAdminUserType : RoleConstants.DefaultUserType;

            var additionalClaims = new Dictionary<string, string>();
            if (user.IsSysAdmin)
                additionalClaims["IsSysAdmin"] = "true";

            var token = additionalClaims.Count > 0
                ? _jwtService.GenerateToken(user.Id.ToString(), user.UserName!, role, additionalClaims, userType)
                : _jwtService.GenerateToken(user.Id.ToString(), user.UserName!, role, userType);

            var userDto = new UserDetailDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                RealName = user.RealName,
                Role = role,
                Status = CommonStatus.Enabled,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                LastLoginTime = user.LastLoginAt
            };

            var response = new LoginResponse
            {
                Token = token,
                User = userDto,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };

            _logger.LogInformation("[AUTH] Login completed - UserName={UserName} Role={Role}",
                request.UserName, role);

            return HandleAuthResult(Result<LoginResponse>.Success(response), "登录成功");
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public IActionResult LogoutAsync([FromBody] LogoutRequest request)
        {
            _logger.LogInformation("[AUTH] Logout - UserName={UserName}", request?.UserName ?? "(unknown)");
            return Success("登出成功");
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public IActionResult RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            if (ValidateModel() is { } modelError) return modelError;

            var result = _jwtService.RefreshToken(request.RefreshToken);
            return HandleAuthResult(result, "Token刷新成功");
        }

        [HttpGet("validate")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public IActionResult ValidateTokenFromHeaderAsync()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Missing Authorization header", new { code = "TokenInvalid" }));
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Invalid Authorization header format", new { code = "TokenInvalid" }));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Missing token in Authorization header", new { code = "TokenInvalid" }));
            }

            var principal = _jwtService.ValidateToken(token);
            if (principal != null)
            {
                object response = new
                {
                    valid = true,
                    sub = new
                    {
                        UserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        UserName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                        Role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    },
                    message = "Token is valid"
                };
                return Success(response, "Token验证成功");
            }

            return Unauthorized(ApiResponse<object>.CreateFail("Token is invalid", new { code = "ERR-10202" }));
        }

        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(405, new { message = "Method Not Allowed - Use POST endpoints for authentication" });
        }

        private static UserRole ParseUserRole(IList<string> roles)
        {
            if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], ignoreCase: true, out var role))
                return role;

            return UserRole.Doctor;
        }
    }
}
