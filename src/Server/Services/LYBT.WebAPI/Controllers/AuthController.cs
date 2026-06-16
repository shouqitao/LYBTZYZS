using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 认证控制器 - 简化版
    /// 提供用户登录、登出和密码管理功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
            : base(logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// 用户登录
        /// </summary>
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

            var result = await _authService.LoginAsync(request);
            return HandleAuthResult(result, "登录成功");
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request)
        {
            if (ValidateModel() is { } modelError) return modelError;

            if (request == null)
                return ValidationFail("登出请求不能为空");

            if (string.IsNullOrWhiteSpace(request.RefreshToken) && string.IsNullOrWhiteSpace(request.UserName))
                return ValidationFail("必须提供RefreshToken或用户名");

            var result = await _authService.LogoutAsync(request);
            return HandleBoolResult(result, "登出成功");
        }

        /// <summary>
        /// 验证Token (GET方法)
        /// </summary>
        [HttpGet("validate")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ValidateTokenFromHeaderAsync()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return Unauthorized(new { valid = false, message = "Missing Authorization header", errorCode = "TokenInvalid" });
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { valid = false, message = "Invalid Authorization header format", errorCode = "TokenInvalid" });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { valid = false, message = "Missing token in Authorization header", errorCode = "TokenInvalid" });
            }

            var result = await _authService.ValidateTokenAsync(token);

            if (result.IsSuccess && result.Data == true)
            {
                var sessionInfo = await _authService.GetSessionInfoAsync(token);
                object response = new
                {
                    valid = true,
                    sub = sessionInfo.Data,
                    message = "Token is valid"
                };
                return Success(response, "Token验证成功");
            }
            else
            {
                var errorCode = result.ModuleErrorCode?.ToFormattedString() ?? "ERR-10202";
                return Unauthorized(new { valid = false, message = result.ErrorMessage ?? "Token is invalid", errorCode });
            }
        }

        /// <summary>
        /// Auth基础端点 - 返回405 Method Not Allowed
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(405, new { message = "Method Not Allowed - Use POST endpoints for authentication" });
        }
    }
}
