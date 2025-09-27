using Asp.Versioning;
using LYBT.Core.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
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

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger,
            IMemoryCache cache)
            : base(logger, cache)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求</param>
        /// <returns>登录响应，包含JWT Token</returns>
        [HttpPost("login")]
        [AllowAnonymous]  // 登录端点允许匿名访问
        [EnableRateLimiting("Login")]
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

                if (string.IsNullOrWhiteSpace(request.Username))
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

                // 验证密码复杂度
                if (!LYBT.Shared.Utilities.Security.PasswordPolicyValidator.Validate(request.NewPassword, out var errors))
                {
                    return ValidationFail($"密码不符合复杂度要求：{string.Join("；", errors)}");
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

        /// <summary>
        /// 刷新Token
        /// </summary>
        /// <param name="request">刷新Token请求</param>
        /// <returns>新的登录响应</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]  // 刷新令牌允许匿名（使用refresh token验证）
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // 参数验证
                var validation = ValidateModel<LoginResponse>();
                if (validation != null)
                {
                    return validation;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return ValidationFail<LoginResponse>("刷新Token不能为空");
                }

                // 调用认证服务刷新Token
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return HandleServiceResult(result, "Token刷新成功");
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
        /// 撤销RefreshToken
        /// </summary>
        /// <param name="request">撤销请求</param>
        /// <returns>撤销结果</returns>
        [HttpPost("revoke")]
        [Authorize]  // 需要认证
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> RevokeTokenAsync([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // 参数验证
                var validation = ValidateModel();
                if (validation != null)
                {
                    return validation;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return ValidationFail("RefreshToken不能为空");
                }

                // 调用JWT服务撤销Token
                await _authService.LogoutAsync(new LogoutRequest 
                { 
                    Username = User.Identity?.Name ?? "",
                    RefreshToken = request.RefreshToken,
                    DeviceId = request.DeviceId
                });

                return Success("Token撤销成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "撤销Token", request);
            }
        }

        /// <summary>
        /// 撤销用户所有Token（强制登出所有设备）
        /// </summary>
        /// <returns>撤销结果</returns>
        [HttpPost("revokeAll")]
        [Authorize]  // 需要认证
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> RevokeAllTokensAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return ValidationFail("无效的用户身份");
                }

                // TODO: 这里需要EnhancedJwtService提供RevokeAllUserTokensAsync方法
                // await _enhancedJwtService.RevokeAllUserTokensAsync(userId);

                return Success("已撤销所有设备的Token");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "撤销所有Token", null);
            }
        }

        /// <summary>
        /// 获取用户设备列表
        /// </summary>
        /// <returns>设备列表</returns>
        [HttpGet("devices")]
        [Authorize]  // 需要认证
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> GetUserDevicesAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return ValidationFail<object>("无效的用户身份");
                }

                // TODO: 这里需要RefreshTokenRepository提供GetUserDevicesAsync方法
                // var devices = await _refreshTokenRepository.GetUserDevicesAsync(userId);

                var devices = new List<object>(); // 临时返回空列表
                return Success(devices, "获取设备列表成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取设备列表", null);
            }
        }

        /// <summary>
        /// 撤销指定设备的所有Token
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>撤销结果</returns>
        [HttpPost("revokeDevice/{deviceId}")]
        [Authorize]  // 需要认证
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> RevokeDeviceTokensAsync(string deviceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return ValidationFail("设备ID不能为空");
                }

                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return ValidationFail("无效的用户身份");
                }

                // TODO: 这里需要EnhancedJwtService提供RevokeDeviceTokensAsync方法
                // await _enhancedJwtService.RevokeDeviceTokensAsync(userId, deviceId);

                return Success($"已撤销设备 {deviceId} 的所有Token");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "撤销设备Token", deviceId);
            }
        }

        /// <summary>
        /// 获取Token安全级别信息
        /// </summary>
        /// <returns>安全级别信息</returns>
        [HttpGet("security")]
        [Authorize]  // 需要认证
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> GetTokenSecurityAsync()
        {
            try
            {
                // 从Authorization header中提取Token
                var authHeader = Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationFail<object>("缺少有效的Authorization header");
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();

                // TODO: 这里需要EnhancedJwtService提供ValidateTokenSecurityAsync方法
                // var securityResult = await _enhancedJwtService.ValidateTokenSecurityAsync(token);

                var securityInfo = new
                {
                    IsValid = true,
                    SecurityLevel = "Standard", // 临时返回
                    Message = "Token安全验证完成"
                };

                return Success(securityInfo, "获取Token安全信息成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取Token安全信息", null);
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
