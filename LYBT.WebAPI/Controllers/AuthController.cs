using Microsoft.AspNetCore.Mvc;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Dtos;
using System.Threading.Tasks;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 认证相关接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) {
            _authService = authService;
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
            return Ok(user);
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
