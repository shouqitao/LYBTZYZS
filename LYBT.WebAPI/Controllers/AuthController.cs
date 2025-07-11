using LYBT.Infrastructure.Auth;
using LYBT.Module.Auth.Dtos;
using LYBT.Common.Responses;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Users.Interfaces;
using System.Security.Claims;
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
        private readonly IUserService _userService;
        private readonly JwtOptions _jwtOptions;

        public AuthController(IAuthService authService, IUserService userService, IOptions<JwtOptions> jwtOptions) {
            _authService = authService;
            _userService = userService;
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

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        [HttpGet("current")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrent() {
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            var user = await _userService.GetByIdAsync(userId);
            return user == null ? Unauthorized(ApiResponse<object>.Fail("未登录", 401)) : Ok(ApiResponse<object>.Success(user));
        }
    }
}