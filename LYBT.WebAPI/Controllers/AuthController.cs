using LYBT.Common.Responses;
using LYBT.Infrastructure.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 认证相关接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase {
        private readonly IAuthService _authService;
        private readonly JwtOptions _jwtOptions;

        public AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions) {
            _authService = authService;
            _jwtOptions = jwtOptions.Value;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("参数无效", 400));
            // 获取客户端IP和UserAgent
            dto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            dto.UserAgent = Request.Headers["User-Agent"].ToString();
            var user = await _authService.LoginAsync(dto);
            if (user == null)
                return Unauthorized(ApiResponse<object>.Fail("用户名或密码错误", 401));

            var token = JwtHelper.GenerateToken(user.Id.ToString(), user.UserName, _jwtOptions);
            return Ok(ApiResponse<LoginResponseDto>.Success(new LoginResponseDto { Token = token, User = user }));
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("参数无效", 400));
            await _authService.LogoutAsync(dto);
            return Ok(ApiResponse<object>.Success(null));
        }

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        [HttpPost("changeSysAdminPassword")]
        public async Task<IActionResult> ChangeSysAdminPassword([FromBody] ChangeSysAdminPasswordDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("参数无效", 400));
            var ok = await _authService.ChangeSysAdminPasswordAsync(dto);
            return ok ? Ok(ApiResponse<object>.Success(null)) : BadRequest(ApiResponse<object>.Fail("修改失败", 400));
        }
    }
}