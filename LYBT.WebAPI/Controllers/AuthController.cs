using LYBT.Infrastructure.Auth;
using LYBT.Module.Auth.Dtos;
using LYBT.Module.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 认证相关接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
                return BadRequest(new ApiResponse<object>(false, "参数无效", null));
            // 获取客户端IP和UserAgent
            dto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            dto.UserAgent = Request.Headers["User-Agent"].ToString();
            var user = await _authService.LoginAsync(dto);
            if (user == null)
                return Unauthorized(new ApiResponse<object>(false, "用户名或密码错误", null));

            var token = JwtHelper.GenerateToken(user.Id.ToString(), user.UserName, _jwtOptions);
            return Ok(new ApiResponse<LoginResponseDto>(true, null, new LoginResponseDto { Token = token, User = user }));
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>(false, "参数无效", null));
            await _authService.LogoutAsync(dto);
            return Ok(new ApiResponse<object>(true, null, null));
        }
    }
}