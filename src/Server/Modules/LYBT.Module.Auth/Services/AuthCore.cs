using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证核心服务 - UltraThink简化版
    /// 合并原AuthServiceCore、AuthQueryService、AuthBusinessService的核心功能
    /// 职责：完整认证流程、密码验证、Token管理、会话处理
    /// </summary>
    public class AuthCore
    {
        private readonly IAuthRepository _authRepository;
        private readonly IAuthSessionRepository _authSessionRepository;
        private readonly IJwtAuthenticationService _jwtAuthenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthCore> _logger;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthCore(
            IAuthRepository authRepository,
            IAuthSessionRepository authSessionRepository,
            IJwtAuthenticationService jwtAuthenticationService,
            IMapper mapper,
            ILogger<AuthCore> logger,
            SysAdminHandler sysAdminHandler)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _authSessionRepository = authSessionRepository ?? throw new ArgumentNullException(nameof(authSessionRepository));
            _jwtAuthenticationService = jwtAuthenticationService ?? throw new ArgumentNullException(nameof(jwtAuthenticationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
        }

        #region 核心认证操作

        /// <summary>
        /// 完整登录流程处理 - UltraThink简化版（5步验证）
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> ProcessLoginAsync(LoginRequest request)
        {
            try
            {
                // 1. 基础参数验证
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("登录参数无效: {Username}", request.Username);
                    return ServiceResult<LoginResponse>.Failure("用户名或密码不能为空");
                }

                // 2. 获取并验证用户信息
                var userResult = await GetUserForAuthenticationAsync(request.Username);
                if (!userResult.IsSuccess || userResult.Data == null)
                {
                    _logger.LogWarning("用户不存在: {Username}", request.Username);
                    return ServiceResult<LoginResponse>.Failure("用户名或密码错误");
                }

                var user = userResult.Data;

                // 3. 验证密码
                var passwordResult = await ValidatePasswordAsync(user, request.Password);
                if (!passwordResult.IsSuccess || !passwordResult.Data)
                {
                    _logger.LogWarning("密码验证失败: {Username}", request.Username);
                    return ServiceResult<LoginResponse>.Failure("用户名或密码错误");
                }

                // 4. 生成JWT Token
                var jwtToken = _jwtAuthenticationService.GenerateToken(
                    user.Id.ToString(),
                    user.Username,
                    user.Role,
                    request.RememberMe);

                // 5. 创建登录响应
                var userDto = CreateUserDto(user);
                var loginResponse = new LoginResponse
                {
                    Token = jwtToken,
                    User = userDto,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(request.RememberMe ? 43200 : 480)
                };

                _logger.LogInformation("用户登录成功: {Username}", user.Username);
                return ServiceResult<LoginResponse>.Success(loginResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录流程处理异常: {Username}", request.Username);
                return ServiceResult<LoginResponse>.Failure("登录过程中发生异常");
            }
        }

        /// <summary>
        /// 用户登出处理 - UltraThink简化版
        /// </summary>
        public async Task<ServiceResult<bool>> ProcessLogoutAsync(LogoutRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return ServiceResult<bool>.Failure("登出请求无效");
                }

                _logger.LogInformation("用户登出: {Username}", request.Username);
                await Task.CompletedTask; // 保持异步接口一致性
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出处理失败: {Username}", request.Username);
                return ServiceResult<bool>.Failure("登出失败");
            }
        }

        #endregion

        #region 核心验证操作

        /// <summary>
        /// 根据用户名获取用户信息（用于认证）
        /// </summary>
        public async Task<ServiceResult<User>> GetUserForAuthenticationAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<User>.Failure("用户名不能为空");
                }

                // 检查是否为系统管理员
                if (username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    var sysAdminUser = await _sysAdminHandler.GetSysAdminUserAsync("sysadmin");
                    if (sysAdminUser != null)
                    {
                        return ServiceResult<User>.Success(sysAdminUser);
                    }
                }

                // 查询普通用户
                var user = await _authRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return ServiceResult<User>.Failure("用户不存在");
                }

                return ServiceResult<User>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户认证信息失败: {Username}", username);
                return ServiceResult<User>.Failure("获取用户信息失败");
            }
        }

        /// <summary>
        /// 验证用户密码
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePasswordAsync(User user, string password)
        {
            try
            {
                if (user == null || string.IsNullOrWhiteSpace(password))
                {
                    return ServiceResult<bool>.Failure("用户信息或密码不能为空");
                }

                // 系统管理员密码验证
                if (user.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    var passwordHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                    var isValidSysAdmin = PasswordHelper.Verify(passwordHash ?? string.Empty, password);
                    return ServiceResult<bool>.Success(isValidSysAdmin);
                }

                // 普通用户密码验证
                var isValid = PasswordHelper.Verify(user.PasswordHash, password);
                return ServiceResult<bool>.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密码验证失败: {Username}", user?.Username);
                return ServiceResult<bool>.Failure("密码验证失败");
            }
        }

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return ServiceResult<bool>.Failure("Token不能为空");
                }

                // 使用JWT服务验证Token
                var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);
                
                await Task.CompletedTask; // 保持异步接口一致性
                return ServiceResult<bool>.Success(claimsPrincipal != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token验证失败");
                return ServiceResult<bool>.Failure("Token验证失败");
            }
        }

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return ServiceResult<object>.Failure("Token不能为空");
                }

                // 验证Token
                var tokenValidation = await ValidateTokenAsync(token);
                if (!tokenValidation.IsSuccess)
                {
                    return ServiceResult<object>.Failure("Token验证失败");
                }

                // 从Token中提取用户ID
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<object>.Failure("无法从Token中提取用户信息");
                }

                // 获取当前用户信息
                var userResult = await GetCurrentUserAsync(userId);
                if (!userResult.IsSuccess)
                {
                    return ServiceResult<object>.Failure("获取用户信息失败");
                }

                // 构建会话信息
                var sessionInfo = new
                {
                    UserId = userId,
                    Username = userResult.Data?.Username,
                    Role = userResult.Data?.Role.ToString(),
                    IsAuthenticated = true,
                    TokenExpiry = DateTime.UtcNow.AddHours(8),
                    LoginTime = DateTime.UtcNow
                };

                return ServiceResult<object>.Success(sessionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话信息失败");
                return ServiceResult<object>.Failure("获取会话信息失败");
            }
        }

        #endregion

        #region 管理员操作

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult<bool>.Failure("新密码不能为空");
                }

                if (newPassword.Length < 8)
                {
                    return ServiceResult<bool>.Failure("密码长度不能少于8位");
                }

                var passwordHash = PasswordHelper.Hash(newPassword);
                await _authRepository.UpdateAdminPasswordHashAsync("sysadmin", passwordHash);

                _logger.LogInformation("系统管理员密码修改成功");
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改系统管理员密码失败");
                return ServiceResult<bool>.Failure("修改系统管理员密码失败");
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 根据用户ID获取当前用户
        /// </summary>
        private async Task<ServiceResult<UserDto>> GetCurrentUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");
                }

                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return ServiceResult<UserDto>.Failure("用户ID格式无效");
                }

                // 检查是否为系统管理员
                if (userId.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    var sysAdminUser = await _sysAdminHandler.GetSysAdminUserAsync("sysadmin");
                    if (sysAdminUser != null)
                    {
                        var sysAdminDto = _mapper.Map<UserDto>(sysAdminUser);
                        return ServiceResult<UserDto>.Success(sysAdminDto);
                    }
                }

                // 获取普通用户
                var user = await _authRepository.GetByUsernameAsync(userId);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var userDto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前用户失败: {UserId}", userId);
                return ServiceResult<UserDto>.Failure("获取当前用户失败");
            }
        }

        /// <summary>
        /// 从Token中提取用户ID
        /// </summary>
        private string ExtractUserIdFromToken(string token)
        {
            try
            {
                var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);
                return claimsPrincipal?.FindFirst("sub")?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Token中提取用户ID失败");
                return string.Empty;
            }
        }

        /// <summary>
        /// 创建用户DTO
        /// </summary>
        private static UserDto CreateUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                RealName = user.RealName,
                Role = user.Role.ToString(),
                Status = user.Status,
                PhoneNumber = user.PhoneNumber
            };
        }

        #endregion
    }
}