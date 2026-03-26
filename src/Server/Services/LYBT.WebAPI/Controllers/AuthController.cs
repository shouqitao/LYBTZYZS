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
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateModel() is { } modelError) return modelError;

            if (request == null)
                return ValidationFail("登录请求不能为空");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return ValidationFail("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.Password))
                return ValidationFail("密码不能为空");

            var result = await _authService.LoginAsync(request, cancellationToken);
            return HandleAuthResult(result, "登录成功");
        }

        /// <summary>
        /// 使用AutoLoginToken自动登录
        /// OpenSpec: refactor-login-authentication (CVT-001)
        /// </summary>
        /// <param name="request">自动登录请求 - 包含用户名和AutoLoginToken</param>
        /// <returns>登录响应 - 包含JWT令牌、用户信息、过期时间、新的AutoLoginToken</returns>
        /// <remarks>
        /// <para>功能: 使用本地存储的AutoLoginToken进行自动登录</para>
        /// <para>安全: AutoLoginToken可被服务端随时撤销，不暴露用户密码</para>
        /// <para>更新: 成功登录后返回新的AutoLoginToken（Token轮换机制）</para>
        /// </remarks>
        [HttpPost("auto-login")]
        [AllowAnonymous]
        [EnableRateLimiting("Login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public async Task<IActionResult> AutoLoginAsync([FromBody] AutoLoginRequest request, CancellationToken cancellationToken = default)
        {
            if (ValidateModel() is { } modelError) return modelError;

            if (request == null)
                return ValidationFail("自动登录请求不能为空");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return ValidationFail("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.AutoLoginToken))
                return ValidationFail("AutoLoginToken不能为空");

            var result = await _authService.LoginWithAutoTokenAsync(request, cancellationToken);
            return HandleAuthResult(result, "自动登录成功");
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateModel() is { } modelError) return modelError;

            if (request == null)
                return ValidationFail("登出请求不能为空");

            if (string.IsNullOrWhiteSpace(request.RefreshToken) && string.IsNullOrWhiteSpace(request.UserName))
                return ValidationFail("必须提供RefreshToken或用户名");

            var result = await _authService.LogoutAsync(request, cancellationToken);
            return HandleBoolResult(result, "登出成功");
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateModel() is { } modelError) return modelError;

            if (request == null)
                return ValidationFail("刷新令牌请求不能为空");

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return ValidationFail("RefreshToken不能为空");

            var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            return HandleAuthResult(result, "Token刷新成功");
        }

        /// <summary>
        /// 验证Token (GET方法)
        /// </summary>
        [HttpGet("validate")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ValidateTokenFromHeaderAsync(CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
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

            var result = await _authService.ValidateTokenAsync(token, cancellationToken);

            if (result.IsSuccess && result.Data == true)
            {
                var sessionInfo = await _authService.GetSessionInfoAsync(token, cancellationToken);
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
