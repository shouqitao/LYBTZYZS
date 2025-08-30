using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证业务逻辑服务 - UltraThink架构
    /// 职责：复杂认证流程、会话管理、安全日志、账户锁定逻辑
    /// </summary>
    public class AuthBusinessService
    {
        private readonly AuthServiceCore _coreService;
        private readonly AuthQueryService _queryService;
        private readonly IAuthSessionRepository _authSessionRepository;
        private readonly IJwtAuthenticationService _jwtAuthenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthBusinessService> _logger;

        public AuthBusinessService(
            AuthServiceCore coreService,
            AuthQueryService queryService,
            IAuthSessionRepository authSessionRepository,
            IJwtAuthenticationService jwtAuthenticationService,
            IMapper mapper,
            ILogger<AuthBusinessService> logger)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _authSessionRepository = authSessionRepository ?? throw new ArgumentNullException(nameof(authSessionRepository));
            _jwtAuthenticationService = jwtAuthenticationService ?? throw new ArgumentNullException(nameof(jwtAuthenticationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 完整登录流程处理
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> ProcessLoginAsync(LoginRequest request)
        {
            try
            {
                // 1. 基础参数验证
                var validationResult = ValidateLoginRequest(request);
                if (!validationResult.IsSuccess)
                {
                    await LogFailedLoginAsync(request.Username, validationResult.ErrorMessage, request);
                    return ServiceResult<LoginResponse>.Failure(validationResult.ErrorMessage);
                }

                // 2. 获取用户信息
                var userResult = await _coreService.GetUserForAuthenticationAsync(request.Username);
                if (!userResult.IsSuccess)
                {
                    await LogFailedLoginAsync(request.Username, "用户不存在", request);
                    return ServiceResult<LoginResponse>.Failure("用户名或密码错误");
                }

                var user = userResult.Data;

                // 3. 检查账户锁定状态
                var lockoutCheck = _coreService.CheckAccountLockout(user);
                if (!lockoutCheck.IsSuccess)
                {
                    await LogFailedLoginAsync(user.Username, lockoutCheck.ErrorMessage, request);
                    return ServiceResult<LoginResponse>.Failure(lockoutCheck.ErrorMessage);
                }

                // 4. 验证用户状态
                var statusCheck = _coreService.ValidateUserStatus(user);
                if (!statusCheck.IsSuccess)
                {
                    await LogFailedLoginAsync(user.Username, statusCheck.ErrorMessage, request);
                    return ServiceResult<LoginResponse>.Failure(statusCheck.ErrorMessage);
                }

                // 5. 验证密码
                var passwordResult = await _coreService.ValidatePasswordAsync(user, request.Password);
                if (!passwordResult.IsSuccess || !passwordResult.Data)
                {
                    await _coreService.RecordLoginFailureAsync(user);
                    await LogFailedLoginAsync(user.Username, "密码错误", request);
                    return ServiceResult<LoginResponse>.Failure("用户名或密码错误");
                }

                // 6. 重置失败尝试计数
                await _coreService.ResetFailedAttemptsAsync(user);

                // 7. 生成JWT Token
                var jwtToken = _jwtAuthenticationService.GenerateToken(
                    user.Id.ToString(),
                    user.Username,
                    user.Role,
                    request.RememberMe);

                // 8. 生成刷新Token
                var refreshToken = await GenerateRefreshTokenAsync(user.Id, request.RememberMe);

                // 9. 创建用户DTO
                var userDto = CreateUserDto(user);

                // 10. 记录成功登录日志
                await LogSuccessfulLoginAsync(user, request);

                // 11. 创建登录响应
                var loginResponse = new LoginResponse
                {
                    Token = jwtToken,
                    User = userDto,
                    RefreshToken = refreshToken.IsSuccess ? refreshToken.Data : string.Empty,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(request.RememberMe ? 43200 : 480)
                };

                return ServiceResult<LoginResponse>.Success(loginResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录流程处理异常: {Username}", request.Username);
                await LogLoginExceptionAsync(request.Username, ex, request);
                return ServiceResult<LoginResponse>.Failure("登录过程中发生异常");
            }
        }

        /// <summary>
        /// 处理用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> ProcessLogoutAsync(LogoutRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Username))
                    return ServiceResult<bool>.Failure("登出请求无效");

                // 验证Token（如果提供了）
                // LogoutRequest 可能没有Token字段，暂时跳过Token验证
                // 在实际项目中需要根据LogoutRequest的实际结构调整
                
                // 使登出的Token失效（如果有Token管理）
                await InvalidateTokenAsync(null);

                // 记录登出日志
                await LogLogoutAsync(request.Username, "用户主动登出");

                _logger.LogInformation("用户成功登出: {Username}", request.Username);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出失败: {Username}", request.Username);
                return ServiceResult<bool>.Failure("登出失败");
            }
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return ServiceResult<LoginResponse>.Failure("刷新令牌不能为空");

                // 验证刷新令牌
                var validationResult = await _queryService.ValidateRefreshTokenAsync(refreshToken);
                if (!validationResult.IsSuccess)
                    return ServiceResult<LoginResponse>.Failure("刷新令牌无效");

                // 从刷新令牌中获取用户信息（这里需要实现具体逻辑）
                // var userId = ExtractUserIdFromRefreshToken(refreshToken);
                // var userResult = await _queryService.GetCurrentUserAsync(userId);
                
                // 暂时返回失败，需要完整的刷新令牌机制
                await LogTokenRefreshAsync(refreshToken, false, "刷新令牌功能待完善");
                return ServiceResult<LoginResponse>.Failure("刷新令牌功能暂未完全实现");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新访问令牌失败");
                await LogTokenRefreshAsync(refreshToken, false, ex.Message);
                return ServiceResult<LoginResponse>.Failure("刷新访问令牌失败");
            }
        }

        /// <summary>
        /// 强制用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> ForceLogoutAsync(string username, string reason = "管理员强制登出")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return ServiceResult<bool>.Failure("用户名不能为空");

                // 使该用户的所有Token失效
                await InvalidateAllUserTokensAsync(username);

                // 记录强制登出日志
                await LogForceLogoutAsync(username, reason);

                _logger.LogInformation("强制用户登出成功: {Username}, 原因: {Reason}", username, reason);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制登出失败: {Username}", username);
                return ServiceResult<bool>.Failure("强制登出失败");
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 验证登录请求
        /// </summary>
        private ServiceResult<bool> ValidateLoginRequest(LoginRequest request)
        {
            if (request == null)
                return ServiceResult<bool>.Failure("登录请求不能为空");

            if (string.IsNullOrWhiteSpace(request.Username))
                return ServiceResult<bool>.Failure("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.Password))
                return ServiceResult<bool>.Failure("密码不能为空");

            if (request.Username.Length < 3)
                return ServiceResult<bool>.Failure("用户名长度不能少于3位");

            if (request.Password.Length < 6)
                return ServiceResult<bool>.Failure("密码长度不能少于6位");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 创建用户DTO
        /// </summary>
        private UserDto CreateUserDto(User user)
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

        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        private async Task<ServiceResult<string>> GenerateRefreshTokenAsync(Guid userId, bool rememberMe)
        {
            try
            {
                var refreshToken = Guid.NewGuid().ToString("N");
                var expiryTime = DateTime.UtcNow.AddDays(rememberMe ? 30 : 7);

                // 这里应该将刷新令牌存储到数据库
                // await _authSessionRepository.SaveRefreshTokenAsync(userId, refreshToken, expiryTime);

                await Task.CompletedTask; // 保持异步接口一致性
                return ServiceResult<string>.Success(refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成刷新令牌失败: {UserId}", userId);
                return ServiceResult<string>.Failure("生成刷新令牌失败");
            }
        }

        /// <summary>
        /// 使令牌失效
        /// </summary>
        private async Task<bool> InvalidateTokenAsync(string? token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return true;

                // 这里应该实现令牌黑名单机制
                // await _authSessionRepository.AddToBlacklistAsync(token);

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使令牌失效异常");
                return false;
            }
        }

        /// <summary>
        /// 使用户的所有令牌失效
        /// </summary>
        private async Task<bool> InvalidateAllUserTokensAsync(string username)
        {
            try
            {
                // 这里应该实现用户所有令牌失效机制
                // await _authSessionRepository.InvalidateAllUserTokensAsync(username);

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用户所有令牌失效异常: {Username}", username);
                return false;
            }
        }

        #endregion

        #region 日志记录方法

        /// <summary>
        /// 记录成功登录日志
        /// </summary>
        private async Task LogSuccessfulLoginAsync(User user, LoginRequest request)
        {
            try
            {
                _logger.LogInformation("用户登录成功: {Username} ({UserId}) from {IP}", 
                    user.Username, user.Id, request.ClientIp ?? "未知");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录成功登录日志失败");
            }
        }

        /// <summary>
        /// 记录失败登录日志
        /// </summary>
        private async Task LogFailedLoginAsync(string username, string reason, LoginRequest request)
        {
            try
            {
                _logger.LogWarning("登录失败: {Username} from {IP}, 原因: {Reason}", 
                    username, request.ClientIp ?? "未知", reason);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录失败登录日志失败");
            }
        }

        /// <summary>
        /// 记录登录异常日志
        /// </summary>
        private async Task LogLoginExceptionAsync(string username, Exception exception, LoginRequest request)
        {
            try
            {
                _logger.LogError(exception, "登录异常: {Username} from {IP}", 
                    username, request.ClientIp ?? "未知");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录登录异常日志失败");
            }
        }

        /// <summary>
        /// 记录登出日志
        /// </summary>
        private async Task LogLogoutAsync(string username, string reason)
        {
            try
            {
                _logger.LogInformation("用户登出: {Username}, 原因: {Reason}", username, reason);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录登出日志失败");
            }
        }

        /// <summary>
        /// 记录强制登出日志
        /// </summary>
        private async Task LogForceLogoutAsync(string username, string reason)
        {
            try
            {
                _logger.LogWarning("强制用户登出: {Username}, 原因: {Reason}", username, reason);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录强制登出日志失败");
            }
        }

        /// <summary>
        /// 记录令牌刷新日志
        /// </summary>
        private async Task LogTokenRefreshAsync(string refreshToken, bool success, string? message = null)
        {
            try
            {
                var tokenPrefix = refreshToken?.Length > 10 ? refreshToken[..10] + "..." : refreshToken;
                if (success)
                {
                    _logger.LogInformation("令牌刷新成功: {Token}", tokenPrefix);
                }
                else
                {
                    _logger.LogWarning("令牌刷新失败: {Token}, 原因: {Message}", tokenPrefix, message);
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录令牌刷新日志失败");
            }
        }

        #endregion
    }
}