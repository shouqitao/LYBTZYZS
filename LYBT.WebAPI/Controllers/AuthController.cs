using Microsoft.AspNetCore.Mvc;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Dtos;
using System.Threading.Tasks;
using LYBT.WebAPI.Services;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 认证相关接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase {
        private readonly IAuthService _authService;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(IAuthService authService, JwtTokenService jwtTokenService) {
            _authService = authService;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var user = await _authService.LoginAsync(dto);
            if (user == null)
                return Unauthorized(new { success = false, message = "用户名或密码错误" });

            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new LoginResponseDto { Token = token, User = user });
        }


        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            await _authService.LogoutAsync(dto);
            return Ok(new { success = true });
        }

    }
}
