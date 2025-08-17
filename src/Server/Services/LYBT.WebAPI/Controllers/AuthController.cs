using Asp.Versioning;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Infrastructure.Web;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 认证相关接口 - 统一API响应格式和错误处理
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize] // 默认需要认证
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IJwtAuthenticationService _jwtService;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthController(
            IAuthService authService,
            IUserService userService,
            IJwtAuthenticationService jwtService,
            SysAdminHandler sysAdminHandler,
            ILogger<AuthController> logger,
            IMemoryCache cache)
            : base(logger, cache)
        {
            _authService = authService;
            _userService = userService;
            _jwtService = jwtService;
            _sysAdminHandler = sysAdminHandler;
        }


        /// <summary>
        /// 用户登录 - 统一API响应格式
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest dto)
        {
            try
            {
                _logger.LogInformation("用户 {Username} 尝试登录", dto?.Username);

                // 参数验证
                if (dto == null)
                    return ValidationFail<LoginResponse>("请求数据不能为空");

                if (string.IsNullOrWhiteSpace(dto.Username))
                    return ValidationFail<LoginResponse>("用户名不能为空");

                if (string.IsNullOrWhiteSpace(dto.Password))
                    return ValidationFail<LoginResponse>("密码不能为空");

                // 设置客户端信息
                dto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                dto.UserAgent = Request.Headers["User-Agent"].ToString();

                // 简化的sysadmin验证
                if (dto.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    return await HandleSysAdminLogin(dto);
                }

                // 普通用户登录
                
                // 1. 先验证身份
                var credentialsResult = await _authService.VerifyCredentialsAsync(dto);
                if (!credentialsResult.IsSuccess)
                {
                    _logger.LogWarning("用户 {Username} 身份验证失败: {Error}", dto.Username, credentialsResult.ErrorMessage);
                    return Unauthorized<LoginResponse>("用户名或密码错误", ApiErrorCodes.AUTHENTICATION_FAILED);
                }
                
                // 2. 获取用户信息
                var userResult = await _userService.GetByUsernameAsync(credentialsResult.Data);
                if (!userResult.IsSuccess || userResult.Data == null)
                {
                    _logger.LogError("身份验证成功但无法获取用户 {Username} 的详细信息", credentialsResult.Data);
                    return InternalError<LoginResponse>("系统错误，请联系管理员", ApiErrorCodes.SYSTEM_CONFIG_ERROR);
                }
                var user = userResult.Data;

                var token = _jwtService.GenerateToken(
                    user.Id.ToString(),
                    user.Username, 
                    new[] { "Admin" }, // Role字段已移除，默认Admin
                    dto.RememberMe
                );

                var response = new LoginResponse
                {
                    Token = token,
                    User = new BaseUser
                    {
                        Id = user.Id,
                        Username = user.Username,
                        RealName = user.RealName,
                        PhoneNumber = user.PhoneNumber,
                        Status = user.Status,
                        CreateTime = user.CreateTime,
                        LastLoginTime = user.LastLoginTime,
                        UpdateTime = user.UpdateTime,
                        Remark = user.Remark
                    }
                };

                LogOperation("用户登录", response.User, user.Id);
                _logger.LogInformation("用户 {Username} 登录成功", dto.Username);
                return Success(response, "登录成功");
            }
            catch (Exception ex)
            {
                return HandleException<LoginResponse>(ex, "用户登录", dto);
            }
        }

        /// <summary>
        /// 专门处理sysadmin登录 - 统一错误处理
        /// </summary>
        private async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> HandleSysAdminLogin(LoginRequest dto)
        {
            try
            {
                // 获取存储的密码哈希
                var storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                if (string.IsNullOrEmpty(storedHash))
                {
                    _logger.LogError("sysadmin密码哈希未找到");
                    return InternalError<LoginResponse>("系统配置错误", ApiErrorCodes.SYSTEM_CONFIG_ERROR);
                }

                // 验证密码
                if (!PasswordHelper.Verify(storedHash, dto.Password))
                {
                    _logger.LogWarning("sysadmin密码验证失败");
                    return Unauthorized<LoginResponse>("密码错误", ApiErrorCodes.AUTHENTICATION_FAILED);
                }

                // 创建用户信息
                var adminUser = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Username = "sysadmin",
                    RealName = "系统管理员",
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

                var response = new LoginResponse
                {
                    Token = token,
                    User = new BaseUser
                    {
                        Id = adminUser.Id,
                        Username = adminUser.Username,
                        RealName = adminUser.RealName,
                        PhoneNumber = adminUser.PhoneNumber,
                        Status = adminUser.Status,
                        CreateTime = adminUser.CreateTime,
                        LastLoginTime = adminUser.LastLoginTime
                    }
                };

                LogOperation("系统管理员登录", response.User, adminUser.Id);
                _logger.LogInformation("sysadmin登录成功");
                return Success(response, "登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "sysadmin登录处理异常");
                return InternalError<LoginResponse>("登录处理异常", ApiErrorCodes.INTERNAL_ERROR);
            }
        }

        /// <summary>
        /// 用户登出 - 统一API响应格式
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> Logout()
        {
            try
            {
                // 从JWT token中获取用户名
                var username = User?.Identity?.Name ?? string.Empty;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("无效的用户身份", ApiErrorCodes.AUTHENTICATION_FAILED);
                }

                var dto = new LogoutRequest
                {
                    Username = username
                };

                await _authService.LogoutAsync(dto);
                
                LogOperation("用户登出", null);
                return Success("登出成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "用户登出", null);
            }
        }

        /// <summary>
        /// 修改sysadmin密码 - 统一API响应格式
        /// </summary>
        [HttpPost("changeSysAdminPassword")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> ChangeSysAdminPassword([FromBody] ChangeSysAdminPassword dto)
        {
            try
            {
                var validation = ValidateModel();
                if (validation != null) return validation;

                var result = await _authService.ChangeSysAdminPasswordAsync(dto);
                if (!result.IsSuccess || !result.Data)
                {
                    return BusinessFail("修改密码失败，请检查当前密码", ApiErrorCodes.PASSWORD_CHANGE_FAILED);
                }

                LogOperation("修改系统管理员密码", null);
                return Success("密码修改成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "修改sysadmin密码", dto);
            }
        }

        /// <summary>
        /// 获取当前用户信息 - 统一API响应格式
        /// </summary>
        [HttpGet("current-user")]
        public ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<BaseUser>> GetCurrentUser()
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();
                if (operatorId == Guid.Empty || string.IsNullOrEmpty(operatorName))
                {
                    return Unauthorized<BaseUser>("无效的用户身份", ApiErrorCodes.AUTHENTICATION_FAILED);
                }

                var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";

                var userInfo = new BaseUser
                {
                    Id = operatorId,
                    Username = operatorName,
                    RealName = operatorName == "sysadmin" ? "系统管理员" : operatorName,
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now
                };

                return Success(userInfo, "获取用户信息成功");
            }
            catch (Exception ex)
            {
                return HandleException<BaseUser>(ex, "获取当前用户信息", null);
            }
        }

        /// <summary>
        /// 刷新JWT令牌 - 统一API响应格式
        /// </summary>
        [HttpPost("refresh-token")]
        public ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>> RefreshToken()
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();
                if (operatorId == Guid.Empty || string.IsNullOrEmpty(operatorName))
                {
                    return Unauthorized<object>("无效的用户身份", ApiErrorCodes.AUTHENTICATION_FAILED);
                }

                var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                
                // 生成新的JWT令牌
                var roles = role != null ? new[] { role } : new string[0];
                var newToken = _jwtService.GenerateToken(operatorId.ToString(), operatorName, roles, false);

                var response = new
                {
                    token = newToken,
                    refreshedAt = DateTime.UtcNow
                };

                LogOperation("刷新令牌", null, operatorId);
                return Success<object>(response, "令牌刷新成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "刷新JWT令牌", null);
            }
        }

        /// <summary>
        /// 修改密码 (通用接口) - 统一API响应格式
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest dto)
        {
            try
            {
                var validation = ValidateModel();
                if (validation != null) return validation;

                var (operatorId, operatorName, _) = GetOperator();
                if (operatorId == Guid.Empty || string.IsNullOrEmpty(operatorName))
                {
                    return Unauthorized("无效的用户身份", ApiErrorCodes.AUTHENTICATION_FAILED);
                }

                // 如果是sysadmin，使用专用的修改密码方法
                if (operatorName.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    var sysAdminDto = new ChangeSysAdminPassword
                    {
                        OldPassword = dto.OldPassword,
                        NewPassword = dto.NewPassword
                    };
                    
                    var result = await _authService.ChangeSysAdminPasswordAsync(sysAdminDto);
                    if (!result.IsSuccess || !result.Data)
                    {
                        return BusinessFail("修改密码失败，请检查当前密码", ApiErrorCodes.PASSWORD_CHANGE_FAILED);
                    }

                    LogOperation("修改密码", null, operatorId);
                    return Success("密码修改成功");
                }

                // 其他用户的密码修改逻辑可以在这里实现
                return BusinessFail("普通用户密码修改功能尚未实现", ApiErrorCodes.FEATURE_NOT_IMPLEMENTED);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "修改密码", dto);
            }
        }
    }
}