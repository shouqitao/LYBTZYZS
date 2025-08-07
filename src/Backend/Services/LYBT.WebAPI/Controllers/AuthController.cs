using Asp.Versioning;
using LYBT.Infrastructure.Authentication;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 认证相关接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize] // 默认需要认证
    public class AuthController : BaseController {
        private readonly IAuthService _authService;
        private readonly IJwtAuthenticationService _jwtService;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthController(
            IAuthService authService,
            IJwtAuthenticationService jwtService,
            SysAdminHandler sysAdminHandler,
            ILogger<AuthController> logger,
            IMemoryCache cache)
            : base(logger, cache) {
            _authService = authService;
            _jwtService = jwtService;
            _sysAdminHandler = sysAdminHandler;
        }

        /// <summary>
        /// 映射共享LoginRequest到本地LoginRequestDto
        /// </summary>
        private LoginRequestDto MapToLocalDto(LYBT.Shared.Models.Auth.LoginRequest sharedDto) {
            return new LoginRequestDto {
                Username = sharedDto.Username,
                Password = sharedDto.Password,
                RememberMe = sharedDto.RememberMe,
                ClientIp = sharedDto.ClientIp,
                UserAgent = sharedDto.UserAgent
            };
        }

        /// <summary>
        /// 用户登录 - 简化版本
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LYBT.Shared.Models.Auth.LoginResponse>> Login([FromBody] LYBT.Shared.Models.Auth.LoginRequest dto) {
            _logger.LogInformation("用户 {Username} 尝试登录", dto.Username);

            // 基本参数验证
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password)) {
                return BadRequest(new ProblemDetails {
                    Title = "参数验证失败",
                    Detail = "用户名和密码不能为空",
                    Status = 400
                });
            }

                // 设置客户端信息
                dto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                dto.UserAgent = Request.Headers["User-Agent"].ToString();

            // 简化的sysadmin验证
            if (dto.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase)) {
                return await HandleSysAdminLogin(dto);
            }

            // 普通用户登录
            var localDto = MapToLocalDto(dto);
            var user = await _authService.LoginAsync(localDto);
            if (user == null) {
                _logger.LogWarning("用户 {Username} 登录失败", dto.Username);
                return Unauthorized(new ProblemDetails {
                    Title = "认证失败",
                    Detail = "用户名或密码错误",
                    Status = 401
                });
            }

            var token = _jwtService.GenerateToken(
                user.Id.ToString(),
                user.Username,
                new[] { "Admin" }, // Role字段已移除，默认Admin
                dto.RememberMe
            );

            var response = new LYBT.Shared.Models.Auth.LoginResponse {
                Token = token,
                User = new LYBT.Shared.Models.Auth.UserInfo {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    Role = "Admin", // Role字段已移除
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.Status == CommonStatus.Enabled
                }
            };

            _logger.LogInformation("用户 {Username} 登录成功", dto.Username);
            return Ok(response);
        }

        /// <summary>
        /// 专门处理sysadmin登录
        /// </summary>
        private async Task<ActionResult<LYBT.Shared.Models.Auth.LoginResponse>> HandleSysAdminLogin(LYBT.Shared.Models.Auth.LoginRequest dto) {
            try {
                // 获取存储的密码哈希
                var storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                if (string.IsNullOrEmpty(storedHash)) {
                    _logger.LogError("sysadmin密码哈希未找到");
                    return StatusCode(500, new ProblemDetails {
                        Title = "系统配置错误",
                        Detail = "系统配置错误",
                        Status = 500
                    });
                }

                // 验证密码
                if (!PasswordHelper.Verify(storedHash, dto.Password)) {
                    _logger.LogWarning("sysadmin密码验证失败");
                    return Unauthorized(new ProblemDetails {
                        Title = "认证失败",
                        Detail = "密码错误",
                        Status = 401
                    });
                }

                // 创建用户信息
                var adminUser = new UserDto {
                    Id = Guid.NewGuid(),
                    Username = "sysadmin",
                    RealName = "系统管理员",
                    // Role = "Admin", // Role字段已移除
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now,
                    LastLoginTime = DateTime.Now
                };

                // 生成JWT令牌
                var token = _jwtService.GenerateToken(
                    adminUser.Id.ToString(),
                    adminUser.Username,
                    new[] { "Admin" }, // Role字段已移除，默认Admin
                    dto.RememberMe
                );

                var response = new LYBT.Shared.Models.Auth.LoginResponse {
                    Token = token,
                    User = new LYBT.Shared.Models.Auth.UserInfo {
                        Id = adminUser.Id,
                        Username = adminUser.Username,
                        RealName = adminUser.RealName,
                        Role = "Admin", // Role字段已移除
                        PhoneNumber = adminUser.PhoneNumber,
                        IsActive = adminUser.Status == CommonStatus.Enabled
                    }
                };

                _logger.LogInformation("sysadmin登录成功");
                return Ok(response);
            } catch (Exception ex) {
                _logger.LogError(ex, "sysadmin登录处理异常");
                return StatusCode(500, new ProblemDetails {
                    Title = "系统错误",
                    Detail = "登录处理异常",
                    Status = 500
                });
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout() {
            // 从JWT token中获取用户名
            var username = User?.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(username)) {
                return Unauthorized(new ProblemDetails {
                    Title = "认证失败",
                    Detail = "无效的用户身份",
                    Status = 401
                });
            }

            var dto = new LogoutRequestDto {
                Username = username
            };

            await _authService.LogoutAsync(dto);
            return Ok(new { message = "登出成功" });
        }

        /// <summary>
        /// 修改sysadmin密码
        /// </summary>
        [HttpPost("changeSysAdminPassword")]
        public async Task<IActionResult> ChangeSysAdminPassword([FromBody] ChangeSysAdminPasswordDto dto) {
            var success = await _authService.ChangeSysAdminPasswordAsync(dto);
            if (!success) {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "修改密码失败，请检查当前密码",
                    Status = 400
                });
            }

            return Ok(new { message = "密码修改成功" });
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("current-user")]
        public ActionResult<LYBT.Shared.Models.Auth.UserInfo> GetCurrentUser() {
            var username = User?.Identity?.Name;
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userId)) {
                return Unauthorized(new ProblemDetails {
                    Title = "认证失败",
                    Detail = "无效的用户身份",
                    Status = 401
                });
            }

            var userRole = role ?? "User"; // 默认角色

            var userInfo = new LYBT.Shared.Models.Auth.UserInfo {
                Id = Guid.Parse(userId),
                Username = username,
                RealName = username == "sysadmin" ? "系统管理员" : username,
                Role = userRole,
                IsActive = true
            };

            return Ok(userInfo);
        }

        /// <summary>
        /// 刷新JWT令牌
        /// </summary>
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken() {
            var username = User?.Identity?.Name;
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userId)) {
                return Unauthorized(new ProblemDetails {
                    Title = "认证失败",
                    Detail = "无效的用户身份",
                    Status = 401
                });
            }

            // 生成新的JWT令牌
            var roles = role != null ? new[] { role } : new string[0];
            var newToken = _jwtService.GenerateToken(userId, username, roles, false);

            var response = new {
                token = newToken,
                refreshedAt = DateTime.UtcNow
            };

            return Ok(response);
        }

        /// <summary>
        /// 修改密码 (通用接口)
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto) {
            var username = User?.Identity?.Name;
            if (string.IsNullOrEmpty(username)) {
                return Unauthorized(new ProblemDetails {
                    Title = "认证失败",
                    Detail = "无效的用户身份",
                    Status = 401
                });
            }

            // 如果是sysadmin，使用专用的修改密码方法
            if (username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase)) {
                var sysAdminDto = new ChangeSysAdminPasswordDto {
                    OldPassword = dto.OldPassword,
                    NewPassword = dto.NewPassword
                };
                var success = await _authService.ChangeSysAdminPasswordAsync(sysAdminDto);
                if (!success) {
                    return BadRequest(new ProblemDetails {
                        Title = "操作失败",
                        Detail = "修改密码失败，请检查当前密码",
                        Status = 400
                    });
                }
                return Ok(new { message = "密码修改成功" });
            }

            // 其他用户的密码修改逻辑可以在这里实现
            return StatusCode(501, new ProblemDetails {
                Title = "功能未实现",
                Detail = "普通用户密码修改功能尚未实现",
                Status = 501
            });
        }
    }
}