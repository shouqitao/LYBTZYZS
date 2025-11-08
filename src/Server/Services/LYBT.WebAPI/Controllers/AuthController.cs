using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 认证控制器 - MVP简化版（Issue #1733 Task 1.4）
    /// 提供用户登录、登出和密码管理功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]  // 默认需要认证，公开端点使用 AllowAnonymous 覆盖
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
        /// <param name="request">登录请求</param>
        /// <returns>登录响应，包含JWT Token</returns>
        [HttpPost("login")]
        [AllowAnonymous]  // 登录端点允许匿名访问
        [EnableRateLimiting("Login")]  // 启用登录限流保护，防暴力破解
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> LoginAsync([FromBody] LoginRequest request)
        {
            try
            {
                // 参数验证
                var validation = ValidateModel<LoginResponse>();
                if (validation != null)
                {
                    return validation;
                }

                if (request == null)
                {
                    return ValidationFail<LoginResponse>("登录请求不能为空");
                }

                if (string.IsNullOrWhiteSpace(request.UserName))
                {
                    return ValidationFail<LoginResponse>("用户名不能为空");
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return ValidationFail<LoginResponse>("密码不能为空");
                }

                // 调用认证服务进行登录
                var result = await _authService.LoginAsync(request);
                return HandleServiceResult(result, "登录成功");
            }
            catch (Exception ex)
            {
                return HandleException<LoginResponse>(ex, "用户登录", request);
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <param name="request">登出请求</param>
        /// <returns>登出结果</returns>
        [HttpPost("logout")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> LogoutAsync([FromBody] LogoutRequest request)
        {
            try
            {
                // 参数验证
                var validation = ValidateModel();
                if (validation != null)
                {
                    return validation;
                }

                if (request == null)
                {
                    return ValidationFail("登出请求不能为空");
                }

                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return ValidationFail("用户名不能为空");
                }

                // 调用认证服务进行登出
                var result = await _authService.LogoutAsync(request);
                return HandleBoolServiceResult(result, "登出成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "用户登出", request);
            }
        }

        // Issue #1909: changeSysAdminPassword端点已移除
        // SuperAdmin现在统一使用UsersController.ChangePassword进行密码修改
        // 通过三角色权限控制确保只有具有相应权限的用户可以修改密码

        /// <summary>
        /// 刷新访问令牌 - Issue #1838
        /// 使用RefreshToken获取新的AccessToken和RefreshToken对
        /// </summary>
        /// <param name="request">刷新令牌请求</param>
        /// <returns>新的令牌对</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]  // 刷新端点允许匿名访问（通过RefreshToken验证）
        [ProducesResponseType(typeof(LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>), 401)]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> RefreshTokenAsync(
            [FromBody] RefreshTokenRequest request)
        {
            try
            {
                // 参数验证
                var validation = ValidateModel<LoginResponse>();
                if (validation != null)
                {
                    return validation;
                }

                if (request == null)
                {
                    return ValidationFail<LoginResponse>("刷新令牌请求不能为空");
                }

                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return ValidationFail<LoginResponse>("RefreshToken不能为空");
                }

                // 调用认证服务刷新Token
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);

                if (!result.IsSuccess)
                {
                    return Unauthorized(LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>.CreateFail(result.ErrorMessage ?? "RefreshToken无效"));
                }

                return Success(result.Data!, "Token刷新成功");
            }
            catch (Exception ex)
            {
                return HandleException<LoginResponse>(ex, "刷新Token", request);
            }
        }

        /// <summary>
        /// 验证Token (GET方法)
        /// 从Authorization header中获取Bearer Token进行验证
        /// </summary>
        /// <returns>验证结果包含token有效性、用户信息和过期时间</returns>
        [HttpGet("validate")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ValidateTokenFromHeaderAsync()
        {
            try
            {
                // 从Authorization header中提取Token
                var authHeader = Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader))
                {
                    return Unauthorized(new { valid = false, message = "Missing Authorization header" });
                }

                // 检查Bearer格式
                if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(new { valid = false, message = "Invalid Authorization header format" });
                }

                // 提取token
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Unauthorized(new { valid = false, message = "Missing token in Authorization header" });
                }

                // 调用认证服务验证Token
                var result = await _authService.ValidateTokenAsync(token);

                if (result.IsSuccess && result.Data == true)
                {
                    // Token有效，返回详细信息
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
                    // Token无效
                    return Unauthorized(new { valid = false, message = result.ErrorMessage ?? "Token is invalid" });
                }
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "验证Token从Header", null);
            }
        }

        /// <summary>
        /// Auth基础端点 - 返回405 Method Not Allowed
        /// 用于冒烟测试验证路由存在
        /// </summary>
        /// <returns>405 Method Not Allowed</returns>
        [HttpGet]
        public ActionResult Get()
        {
            return StatusCode(405, new { message = "Method Not Allowed - Use POST endpoints for authentication" });
        }
    }
}
