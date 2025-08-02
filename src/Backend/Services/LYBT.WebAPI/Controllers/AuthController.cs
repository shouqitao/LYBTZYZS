using Asp.Versioning;
using LYBT.Common.Helpers;
using LYBT.Infrastructure.Authentication;
using LYBT.Models.Auth;
using LYBT.Models.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Auth;
using LYBT.Shared.Models.Enums;
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
        public async Task<LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>> Login([FromBody] LYBT.Shared.Models.Auth.LoginRequest dto) {
            try {
                _logger.LogInformation("用户 {Username} 尝试登录", dto.Username);

                // 基本参数验证
                if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password)) {
                    return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Fail("用户名和密码不能为空", 400);
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
                    return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Fail("用户名或密码错误", 401);
                }

                var token = _jwtService.GenerateToken(
                    user.Id.ToString(), 
                    user.Username, 
                    new[] { user.Role.ToString() }, 
                    dto.RememberMe
                );

                var response = new LYBT.Shared.Models.Auth.LoginResponse { 
                    Token = token, 
                    User = new LYBT.Shared.Models.Auth.UserInfo {
                        Id = user.Id,
                        UserName = user.Username,
                        RealName = user.RealName,
                        Role = user.Role.ToString(),  
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        IsActive = user.IsActive
                    }
                };

                _logger.LogInformation("用户 {Username} 登录成功", dto.Username);
                return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Success(response);

            } catch (Exception ex) {
                _logger.LogError(ex, "用户 {Username} 登录过程发生异常", dto.Username);
                return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Fail($"登录失败: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// 专门处理sysadmin登录
        /// </summary>
        private async Task<LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>> HandleSysAdminLogin(LYBT.Shared.Models.Auth.LoginRequest dto) {
            try {
                // 获取存储的密码哈希
                var storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                if (string.IsNullOrEmpty(storedHash)) {
                    _logger.LogError("sysadmin密码哈希未找到");
                    return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Fail("系统配置错误", 500);
                }

                // 验证密码
                if (!PasswordHelper.Verify(storedHash, dto.Password)) {
                    _logger.LogWarning("sysadmin密码验证失败");
                    return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Fail("密码错误", 401);
                }

                // 创建用户信息
                var adminUser = new UserDto {
                    Id = Guid.NewGuid(),
                    Username = "sysadmin",
                    RealName = "系统管理员", 
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreateTime = DateTime.Now,
                    LastLoginTime = DateTime.Now
                };

                // 生成JWT令牌
                var token = _jwtService.GenerateToken(
                    adminUser.Id.ToString(),
                    adminUser.Username,
                    new[] { adminUser.Role.ToString() },
                    dto.RememberMe
                );

                var response = new LYBT.Shared.Models.Auth.LoginResponse {
                    Token = token,
                    User = new LYBT.Shared.Models.Auth.UserInfo {
                        Id = adminUser.Id,
                        UserName = adminUser.Username,
                        RealName = adminUser.RealName,
                        Role = adminUser.Role.ToString(),
                        Email = adminUser.Email,
                        PhoneNumber = adminUser.PhoneNumber,
                        IsActive = adminUser.IsActive
                    }
                };

                _logger.LogInformation("sysadmin登录成功");
                return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Success(response);

            } catch (Exception ex) {
                _logger.LogError(ex, "sysadmin登录处理异常");
                return LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>.Fail("登录处理异常", 500);
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<LYBT.Shared.Models.Common.ApiResponse<object>> Logout() {
            try {
                // 从JWT token中获取用户名
                var username = User?.Identity?.Name ?? string.Empty;
                if (string.IsNullOrEmpty(username)) {
                    return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("无效的用户身份", 401);
                }

                var dto = new LogoutRequestDto {
                    Username = username
                };
                
                await _authService.LogoutAsync(dto);
                return LYBT.Shared.Models.Common.ApiResponse<object>.Success(new { }, "登出成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "用户登出异常");
                return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("登出失败", 500);
            }
        }

        /// <summary>
        /// 修改sysadmin密码
        /// </summary>
        [HttpPost("changeSysAdminPassword")]
        public async Task<LYBT.Shared.Models.Common.ApiResponse<object>> ChangeSysAdminPassword([FromBody] ChangeSysAdminPasswordDto dto) {
            try {
                var success = await _authService.ChangeSysAdminPasswordAsync(dto);
                if (!success) {
                    return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("修改密码失败，请检查当前密码", 400);
                }

                return LYBT.Shared.Models.Common.ApiResponse<object>.Success(new { }, "密码修改成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "修改sysadmin密码异常");
                return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("密码修改失败", 500);
            }
        }

        /// <summary>
        /// 刷新JWT令牌
        /// </summary>
        [HttpPost("RefreshToken")]
        public Task<LYBT.Shared.Models.Common.ApiResponse<object>> RefreshToken() {
            try {
                var username = User?.Identity?.Name;
                var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userId)) {
                    return Task.FromResult(LYBT.Shared.Models.Common.ApiResponse<object>.Fail("无效的用户身份", 401));
                }

                // 生成新的JWT令牌
                var roles = role != null ? new[] { role } : new string[0];
                var newToken = _jwtService.GenerateToken(userId, username, roles, false);
                
                var response = new {
                    Token = newToken,
                    RefreshedAt = DateTime.UtcNow
                };

                return Task.FromResult(LYBT.Shared.Models.Common.ApiResponse<object>.Success(response, "令牌刷新成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "刷新令牌异常");
                return Task.FromResult(LYBT.Shared.Models.Common.ApiResponse<object>.Fail("刷新令牌失败", 500));
            }
        }

        /// <summary>
        /// 修改密码 (通用接口)
        /// </summary>
        [HttpPost("ChangePassword")]
        public async Task<LYBT.Shared.Models.Common.ApiResponse<object>> ChangePassword([FromBody] ChangePasswordRequestDto dto) {
            try {
                var username = User?.Identity?.Name;
                if (string.IsNullOrEmpty(username)) {
                    return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("无效的用户身份", 401);
                }

                // 如果是sysadmin，使用专用的修改密码方法
                if (username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase)) {
                    var sysAdminDto = new ChangeSysAdminPasswordDto {
                        OldPassword = dto.OldPassword,
                        NewPassword = dto.NewPassword
                    };
                    var success = await _authService.ChangeSysAdminPasswordAsync(sysAdminDto);
                    if (!success) {
                        return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("修改密码失败，请检查当前密码", 400);
                    }
                    return LYBT.Shared.Models.Common.ApiResponse<object>.Success(new { }, "密码修改成功");
                }

                // 其他用户的密码修改逻辑可以在这里实现
                return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("普通用户密码修改功能尚未实现", 501);
            } catch (Exception ex) {
                _logger.LogError(ex, "修改密码异常");
                return LYBT.Shared.Models.Common.ApiResponse<object>.Fail("密码修改失败", 500);
            }
        }

    }
}