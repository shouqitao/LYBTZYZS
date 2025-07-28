using LYBT.Common.Responses;
using LYBT.Infrastructure.Authentication;
using LYBT.Infrastructure.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 认证相关接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : BaseController {
        private readonly IAuthService _authService;
        private readonly IJwtAuthenticationService _jwtService;

        public AuthController(
            IAuthService authService, 
            IJwtAuthenticationService jwtService,
            ILogger<AuthController> logger,
            IMemoryCache cache) 
            : base(logger, cache) {
            _authService = authService;
            _jwtService = jwtService;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto dto) {
            try {
                var validationResult = ValidateModel<LoginResponseDto>();
                if (validationResult != null) return validationResult;

                // 获取客户端IP和UserAgent
                dto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                dto.UserAgent = Request.Headers["User-Agent"].ToString();
                
                var user = await _authService.LoginAsync(dto);
                if (user == null) {
                    return Unauthorized(ApiResponse<LoginResponseDto>.Fail("用户名或密码错误", 401));
                }

                var token = _jwtService.GenerateToken(user.Id.ToString(), user.UserName, user.Roles.Select(r => r.ToString()));
                var response = new LoginResponseDto { Token = token, User = user };
                
                LogOperation("用户登录成功", new { UserId = user.Id, UserName = user.UserName });
                
                return Ok(ApiResponse<LoginResponseDto>.Success(response));
            } catch (Exception ex) {
                return HandleException<LoginResponseDto>(ex, "用户登录", new { dto.UserName });
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequestDto dto) {
            try {
                var validationResult = ValidateModel<object>();
                if (validationResult != null) return validationResult;

                await _authService.LogoutAsync(dto);
                
                LogOperation("用户登出", dto);
                
                return Ok(ApiResponse<object>.Success(null));
            } catch (Exception ex) {
                return HandleException<object>(ex, "用户登出", dto);
            }
        }

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        [HttpPost("changeSysAdminPassword")]
        public async Task<ActionResult<ApiResponse<object>>> ChangeSysAdminPassword([FromBody] ChangeSysAdminPasswordDto dto) {
            try {
                var validationResult = ValidateModel<object>();
                if (validationResult != null) return validationResult;

                var success = await _authService.ChangeSysAdminPasswordAsync(dto);
                if (!success) {
                    return BadRequest(ApiResponse<object>.Fail("修改密码失败，请检查当前密码是否正确", 400));
                }
                
                LogOperation("管理员密码修改", new { dto.UserName });
                
                return Ok(ApiResponse<object>.Success(null, "密码修改成功"));
            } catch (Exception ex) {
                return HandleException<object>(ex, "修改管理员密码", new { dto.UserName });
            }
        }
    }
}