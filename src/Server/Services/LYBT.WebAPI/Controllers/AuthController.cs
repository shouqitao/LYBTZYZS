using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Common;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 认证控制器 - UltraThink v2.0 精简版
    /// 提供用户登录、登出和密码管理功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> LoginAsync([FromBody] LoginRequest request)
        {
            try
            {
                // 参数验证
                var validation = ValidateModel<LoginResponse>();
                if (validation != null) return validation;

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
                if (validation != null) return validation;

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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> ChangeSysAdminPasswordAsync([FromBody] ChangeSysAdminPassword request)
        {
            try
            {
                // 参数验证
                var validation = ValidateModel();
                if (validation != null) return validation;

                if (request == null)
                {
                    return ValidationFail("修改密码请求不能为空");
                }

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return ValidationFail("新密码不能为空");
                }

                if (request.NewPassword.Length < 6)
                {
                    return ValidationFail("新密码长度不能小于6位");
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
        /// <param name="refreshToken">刷新Token</param>
        /// <returns>新的登录响应</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> RefreshTokenAsync([FromBody] string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return ValidationFail<LoginResponse>("刷新Token不能为空");
                }

                // 调用认证服务刷新Token
                var result = await _authService.RefreshTokenAsync(refreshToken);
                return HandleServiceResult(result, "Token刷新成功");
            }
            catch (Exception ex)
            {
                return HandleException<LoginResponse>(ex, "刷新Token", refreshToken);
            }
        }

        /// <summary>
        /// 验证Token
        /// </summary>
        /// <param name="token">要验证的Token</param>
        /// <returns>验证结果</returns>
        [HttpPost("validate")]
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
    }
}