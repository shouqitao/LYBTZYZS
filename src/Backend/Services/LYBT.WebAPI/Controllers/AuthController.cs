using Asp.Versioning;
using LYBT.Common.Responses;
using LYBT.Common.Helpers;
using LYBT.Infrastructure.Authentication;
using LYBT.Models.Auth;
using LYBT.Models.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
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
        /// 用户登录 - 简化版本
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ApiResponse<LoginResponseDto>> Login([FromBody] LoginRequestDto dto) {
            try {
                _logger.LogInformation("用户 {Username} 尝试登录", dto.Username);

                // 基本参数验证
                if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password)) {
                    return ApiResponse<LoginResponseDto>.Fail("用户名和密码不能为空", 400);
                }

                // 设置客户端信息
                dto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                dto.UserAgent = Request.Headers["User-Agent"].ToString();

                // 简化的sysadmin验证
                if (dto.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase)) {
                    return await HandleSysAdminLogin(dto);
                }

                // 普通用户登录
                var user = await _authService.LoginAsync(dto);
                if (user == null) {
                    _logger.LogWarning("用户 {Username} 登录失败", dto.Username);
                    return ApiResponse<LoginResponseDto>.Fail("用户名或密码错误", 401);
                }

                var token = _jwtService.GenerateToken(
                    user.Id.ToString(), 
                    user.UserName, 
                    new[] { user.Role.ToString() }, 
                    dto.RememberMe
                );

                var response = new LoginResponseDto { 
                    Token = token, 
                    User = user 
                };

                _logger.LogInformation("用户 {Username} 登录成功", dto.Username);
                return ApiResponse<LoginResponseDto>.Success(response);

            } catch (Exception ex) {
                _logger.LogError(ex, "用户 {Username} 登录过程发生异常", dto.Username);
                return ApiResponse<LoginResponseDto>.Fail($"登录失败: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// 专门处理sysadmin登录
        /// </summary>
        private async Task<ApiResponse<LoginResponseDto>> HandleSysAdminLogin(LoginRequestDto dto) {
            try {
                // 获取存储的密码哈希
                var storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                if (string.IsNullOrEmpty(storedHash)) {
                    _logger.LogError("sysadmin密码哈希未找到");
                    return ApiResponse<LoginResponseDto>.Fail("系统配置错误", 500);
                }

                // 验证密码
                if (!PasswordHelper.Verify(storedHash, dto.Password)) {
                    _logger.LogWarning("sysadmin密码验证失败");
                    return ApiResponse<LoginResponseDto>.Fail("密码错误", 401);
                }

                // 创建用户信息
                var adminUser = new UserDto {
                    Id = Guid.NewGuid(),
                    UserName = "sysadmin",
                    RealName = "系统管理员", 
                    Role = Common.Enums.Users.UserRole.Admin,
                    IsActive = true,
                    CreatedTime = DateTime.Now,
                    LastLoginTime = DateTime.Now,
                    IsSuperAdmin = true
                };

                // 生成JWT令牌
                var token = _jwtService.GenerateToken(
                    adminUser.Id.ToString(),
                    adminUser.UserName,
                    new[] { adminUser.Role.ToString() },
                    dto.RememberMe
                );

                var response = new LoginResponseDto {
                    Token = token,
                    User = adminUser
                };

                _logger.LogInformation("sysadmin登录成功");
                return ApiResponse<LoginResponseDto>.Success(response);

            } catch (Exception ex) {
                _logger.LogError(ex, "sysadmin登录处理异常");
                return ApiResponse<LoginResponseDto>.Fail("登录处理异常", 500);
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<ApiResponse<object>> Logout([FromBody] LogoutRequestDto dto) {
            try {
                await _authService.LogoutAsync(dto);
                return ApiResponse<object>.Success(new { }, "登出成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "用户登出异常");
                return ApiResponse<object>.Fail("登出失败", 500);
            }
        }

        /// <summary>
        /// 修改sysadmin密码
        /// </summary>
        [HttpPost("changeSysAdminPassword")]
        public async Task<ApiResponse<object>> ChangeSysAdminPassword([FromBody] ChangeSysAdminPasswordDto dto) {
            try {
                var success = await _authService.ChangeSysAdminPasswordAsync(dto);
                if (!success) {
                    return ApiResponse<object>.Fail("修改密码失败，请检查当前密码", 400);
                }

                return ApiResponse<object>.Success(new { }, "密码修改成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "修改sysadmin密码异常");
                return ApiResponse<object>.Fail("密码修改失败", 500);
            }
        }

        /// <summary>
        /// 健康检查登录 - 调试用
        /// </summary>
        [HttpPost("testLogin")]
        [AllowAnonymous]
        public async Task<ApiResponse<object>> TestLogin([FromBody] LoginRequestDto dto) {
            try {
                if (dto.Username != "sysadmin") {
                    return ApiResponse<object>.Fail("仅支持sysadmin测试", 400);
                }

                var storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                var isValid = !string.IsNullOrEmpty(storedHash) && PasswordHelper.Verify(storedHash, dto.Password);

                return ApiResponse<object>.Success(new {
                    Username = dto.Username,
                    HasStoredHash = !string.IsNullOrEmpty(storedHash),
                    HashLength = storedHash?.Length ?? 0,
                    PasswordValid = isValid,
                    TestTime = DateTime.Now
                });

            } catch (Exception ex) {
                return ApiResponse<object>.Fail($"测试异常: {ex.Message}", 500);
            }
        }
    }
}