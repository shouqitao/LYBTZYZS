using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 认证控制器 - MVP简化版
    /// 提供用户登录、登出和密码管理功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration Configuration;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger,
            IConfiguration configuration)
            : base(logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
            try
            {
                if (ValidateModel() is { } modelError) return modelError;

                if (request == null)
                    return ValidationFail("登录请求不能为空");

                if (string.IsNullOrWhiteSpace(request.UserName))
                    return ValidationFail("用户名不能为空");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return ValidationFail("密码不能为空");

                var result = await _authService.LoginAsync(request);
                // Issue #1864: 使用HandleAuthResult根据ErrorCode返回正确HTTP状态码
                return HandleAuthResult(result, "登录成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "用户登录", request);
            }
        }

        /// <summary>
        /// 用户登出
        /// Issue #1864 AUTH-008: 允许过期Token登出，确保服务端会话被正确清理
        /// Issue #1864 AUTH-009: Logout后必须重新登录，不支持会话恢复
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous] // Issue #1864: 允许过期Token访问登出端点
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request)
        {
            try
            {
                if (ValidateModel() is { } modelError) return modelError;

                if (request == null)
                    return ValidationFail("登出请求不能为空");

                // Issue #1864: 登出请求必须提供RefreshToken或用户名
                // RefreshToken用于精确定位会话，用户名用于审计日志
                if (string.IsNullOrWhiteSpace(request.RefreshToken) && string.IsNullOrWhiteSpace(request.UserName))
                    return ValidationFail("必须提供RefreshToken或用户名");

                var result = await _authService.LogoutAsync(request);
                return HandleBoolResult(result, "登出成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "用户登出", request);
            }
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (ValidateModel() is { } modelError) return modelError;

                if (request == null)
                    return ValidationFail("刷新令牌请求不能为空");

                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return ValidationFail("RefreshToken不能为空");

                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                // Issue #1864: 使用HandleAuthResult根据ErrorCode返回正确HTTP状态码
                // TokenRevoked/RefreshTokenExpired/RefreshTokenInvalid -> 401
                return HandleAuthResult(result, "Token刷新成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "刷新Token", request);
            }
        }

        /// <summary>
        /// 验证Token (GET方法)
        /// </summary>
        [HttpGet("validate")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ValidateTokenFromHeaderAsync()
        {
            try
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
                    // Issue #1864: 返回结构化错误码
                    var errorCode = result.ErrorCode?.ToString() ?? "TokenInvalid";
                    return Unauthorized(new { valid = false, message = result.ErrorMessage ?? "Token is invalid", errorCode });
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "验证Token从Header", null);
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
