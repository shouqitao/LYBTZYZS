using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 认证控制器 - UltraThink v2.0 精简版
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
            IMemoryCache cache,
            IConfiguration configuration)
            : base(logger, cache)
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
        /// 超级管理员登录（隐藏端点）
        /// 专用的超级管理员登录接口，用户名从配置读取，只需提供密码
        /// </summary>
        /// <param name="request">超级管理员登录请求（只包含密码）</param>
        /// <returns>登录响应，包含JWT Token</returns>
        [HttpPost("admin/login")]
        [AllowAnonymous]  // 登录端点允许匿名访问
        [EnableRateLimiting("Login")]  // 启用登录限流保护，防暴力破解
        [ApiExplorerSettings(IgnoreApi = true)]  // 从Swagger文档中隐藏此端点
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> SuperAdminLoginAsync([FromBody] SuperAdminLoginRequest request)
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

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return ValidationFail<LoginResponse>("密码不能为空");
                }

                // 从配置获取超级管理员用户名
                var sysAdminUsername = Configuration["Lybt:Business:SystemAdmin:UserName"] ?? "clinic_admin";

                // 构造标准登录请求
                var loginRequest = new LoginRequest
                {
                    UserName = sysAdminUsername,
                    Password = request.Password,
                    RememberMe = false
                };

                // 调用认证服务进行登录
                var result = await _authService.LoginAsync(loginRequest);

                // 如果登录成功且是超级管理员，返回成功
                if (result.IsSuccess && result.Data != null && result.Data.User.Id == Guid.Empty)
                {
                    return HandleServiceResult(result, "超级管理员登录成功");
                }

                // 登录失败或不是超级管理员
                return ValidationFail<LoginResponse>("认证失败");
            }
            catch (Exception ex)
            {
                return HandleException<LoginResponse>(ex, "超级管理员登录", request);
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

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        /// <param name="request">修改密码请求</param>
        /// <returns>修改结果</returns>
        [HttpPost("changeSysAdminPassword")]
        [Authorize(Roles = "Admin")]  // 仅管理员可访问
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> ChangeSysAdminPasswordAsync([FromBody] ChangeSysAdminPassword request)
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
                    return ValidationFail("修改密码请求不能为空");
                }

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return ValidationFail("新密码不能为空");
                }

                // 简化密码验证：仅检查长度（适度设计原则）
                if (request.NewPassword.Length < 6)
                {
                    return ValidationFail("新密码长度不能少于6位");
                }

                // 调用认证服务修改密码
                var result = await _authService.ChangeSysAdminPasswordAsync(request);
                return HandleBoolServiceResult(result, "密码修改成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "修改密码", request);
            }
        }

        // 移除刷新Token和撤销Token端点
        // 简化版本不支持刷新令牌机制，遵循适度设计原则

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
        /// 验证Token (POST方法)
        /// </summary>
        /// <param name="token">要验证的Token</param>
        /// <returns>验证结果</returns>
        [HttpPost("validate")]
        [AllowAnonymous]  // Token验证端点需要允许匿名访问（通过参数传递token）
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<bool>>> ValidateTokenAsync([FromBody] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return ValidationFail<bool>("Token不能为空");
                }

                // 调用认证服务验证Token
                var result = await _authService.ValidateTokenAsync(token);
                return HandleServiceResult(result, "Token验证完成");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "验证Token", token);
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
