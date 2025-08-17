using LYBT.Infrastructure.Options;
using AutoMapper;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Auth.Services
{

    /// <summary>
    /// 认证模块核心服务 - UltraThink Phase 6: 实现Shared接口统一
    /// 实现用户登录验证、令牌生成及登录日志记录
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        
        private readonly SysAdminHandler _sysAdminHandler;
        private readonly AuthOptions _authOptions;
        private readonly ILogger<AuthService> _logger;
        private readonly ILoginAttemptService _loginAttemptService;

        public AuthService(
            IAuthRepository authRepository,
            IMapper mapper,
            SysAdminHandler sysAdminHandler,
            IOptions<AuthOptions> authOptions,
            ILogger<AuthService> logger,
            ILoginAttemptService loginAttemptService)
        {
            _authRepository = authRepository;
            _mapper = mapper;
            _sysAdminHandler = sysAdminHandler;
            _authOptions = authOptions.Value;
            _logger = logger;
            _loginAttemptService = loginAttemptService;
        }

        #region Shared Interface Implementation

        /// <summary>
        /// [Shared] 用户登录验证
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // 验证凭据
                var credentialsResult = await VerifyCredentialsAsync(request);
                if (!credentialsResult.IsSuccess)
                {
                    return ServiceResult<LoginResponse>.Failure(credentialsResult.ErrorMessage ?? "登录失败", credentialsResult.Exception);
                }

                // 获取用户信息用于创建LoginResponse
                var user = await GetUserForAuthentication(request.Username);
                if (user == null)
                {
                    return ServiceResult<LoginResponse>.Failure("用户不存在");
                }

                // 处理登录成功，更新用户状态
                var userDto = await HandleSuccessfulLoginAsync(user, request);

                // 创建登录响应（简化实现，实际项目中应该包含JWT token生成）
                var loginResponse = new LoginResponse
                {
                    Token = $"mock_token_{Guid.NewGuid()}", // 实际应该生成JWT
                    User = _mapper.Map<BaseUser>(userDto)
                };

                return ServiceResult<LoginResponse>.Success(loginResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录过程中发生异常: {Username}", request.Username);
                return ServiceResult<LoginResponse>.Failure("登录过程中发生异常", ex);
            }
        }

        /// <summary>
        /// [Shared] 用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
        {
            try
            {
                var result = await LogoutInternalAsync(request);
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出失败: {Username}", request.Username);
                return ServiceResult<bool>.Failure("登出失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 修改sysadmin密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        {
            try
            {
                var result = await ChangeSysAdminPasswordInternalAsync(request);
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改sysadmin密码失败");
                return ServiceResult<bool>.Failure("修改密码失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 验证用户凭据
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
        {
            try
            {
                var username = await VerifyCredentialsInternalAsync(request);
                if (username != null)
                {
                    return ServiceResult<string>.Success(username);
                }
                else
                {
                    return ServiceResult<string>.Failure("用户名或密码错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证凭据失败: {Username}", request.Username);
                return ServiceResult<string>.Failure("验证凭据失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 刷新Token
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // 简化实现，实际项目中应该验证刷新token并生成新的token
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return ServiceResult<LoginResponse>.Failure("刷新token无效");
                }

                // 模拟刷新token逻辑
                var newLoginResponse = new LoginResponse
                {
                    Token = $"new_token_{Guid.NewGuid()}",
                    User = new BaseUser() // 简化实现，实际应该从token中解析用户信息
                };

                return ServiceResult<LoginResponse>.Success(newLoginResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token失败");
                return ServiceResult<LoginResponse>.Failure("刷新Token失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 验证Token有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            try
            {
                // 简化实现，实际项目中应该验证JWT token
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<bool>.Success(false);
                }

                // 模拟token验证逻辑
                var isValid = token.StartsWith("mock_token_") || token.StartsWith("new_token_");
                return ServiceResult<bool>.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证Token失败");
                return ServiceResult<bool>.Failure("验证Token失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 获取用户会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            try
            {
                // 简化实现，实际项目中应该从token中解析用户信息
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<object>.Failure("Token无效");
                }

                var sessionInfo = new
                {
                    UserId = Guid.NewGuid(),
                    Username = "unknown",
                    ExpiresAt = DateTime.UtcNow.AddHours(8),
                    IsValid = true
                };

                return ServiceResult<object>.Success(sessionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话信息失败");
                return ServiceResult<object>.Failure("获取会话信息失败", ex);
            }
        }

        #endregion

        #region Legacy Internal Methods (保持兼容性)

        /// <summary>
        /// 验证用户名和密码，成功返回用户名，失败返回null
        /// </summary>
        private async Task<string?> VerifyCredentialsInternalAsync(LoginRequest dto)
        {
            try
            {
                // 1. 检查账户是否被锁定（防暴力破解）
                if (await _loginAttemptService.IsAccountLockedAsync(dto.Username))
                {
                    var remainingSeconds = await _loginAttemptService.GetRemainingLockTimeAsync(dto.Username);
                    var remainingMinutes = Math.Ceiling(remainingSeconds / 60.0);
                    await LogFailedLogin(Guid.Empty, dto.Username, $"账户已被锁定，请{remainingMinutes}分钟后再试", dto);
                    return null;
                }

                // 2. 验证登录类型
                var loginTypeValidation = ValidateLoginType(dto);
                if (!loginTypeValidation.IsValid)
                {
                    await LogFailedLogin(Guid.Empty, dto.Username, loginTypeValidation.ErrorMessage, dto);
                    return null;
                }

                // 3. 获取用户信息
                var user = await GetUserForAuthentication(dto.Username);
                if (user == null)
                {
                    await _loginAttemptService.RecordLoginAttemptAsync(dto.Username, false, "用户不存在或未启用");
                    await LogFailedLogin(Guid.Empty, dto.Username, "用户不存在或未启用", dto);
                    return null;
                }

                // 4. 检查账户状态
                var lockoutCheck = CheckAccountLockout(user);
                if (lockoutCheck.IsLocked)
                {
                    await LogFailedLogin(user.Id, user.RealName, lockoutCheck.ErrorMessage, dto);
                    return null;
                }

                // 5. 验证密码
                var passwordValidation = await ValidatePasswordAsync(user, dto.Password);
                if (!passwordValidation.IsValid)
                {
                    await _loginAttemptService.RecordLoginAttemptAsync(dto.Username, false, passwordValidation.ErrorMessage, user.Id);
                    await HandleFailedLoginAsync(user, dto, passwordValidation.ErrorMessage);
                    return null;
                }

                // 6. 身份验证成功，清除失败尝试记录并记录登录日志
                await _loginAttemptService.ClearFailedAttemptsAsync(dto.Username);
                await LogSuccessfulLogin(user, dto);
                return dto.Username; // 返回用户名而不是用户详情
            }
            catch (Exception ex)
            {
                await LogLoginException(dto.Username, ex, dto);
                throw;
            }
        }

        /// <summary>
        /// 用户登出并写入日志
        /// </summary>
        private async Task<bool> LogoutInternalAsync(LogoutRequest dto)
        {
            var user = await _authRepository.GetByUsernameAsync(dto.Username);
            var operatorName = GetOperatorName(user, dto.Username);

            await LogUserAction(
                user?.Id ?? Guid.Empty,
                operatorName,
                ActionType.Logout,
                "用户登出"
            );

            return true;
        }

        /// <summary>
        /// 修改系统管理员密码，先校验旧密码
        /// </summary>
        private async Task<bool> ChangeSysAdminPasswordInternalAsync(ChangeSysAdminPassword dto)
        {
            var currentHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
            if (string.IsNullOrEmpty(currentHash))
            {
                return false;
            }

            if (!PasswordHelper.Verify(currentHash, dto.OldPassword))
            {
                return false;
            }

            var newHash = PasswordHelper.Hash(dto.NewPassword);
            await _authRepository.UpdateAdminPasswordHashAsync("sysadmin", newHash);

            await LogUserAction(
                Guid.Empty,
                "sysadmin",
                ActionType.Update,
                "修改系统管理员密码"
            );

            return true;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 验证登录类型
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateLoginType(LoginRequest dto)
        {
            if (string.IsNullOrEmpty(dto.LoginType))
            {
                dto.LoginType = "Password";
            }

            if (!_authOptions.SupportedLoginTypes.Contains(dto.LoginType))
            {
                return (false, $"不支持的登录类型: {dto.LoginType}");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 获取用于认证的用户信息
        /// </summary>
        private async Task<UserModel?> GetUserForAuthentication(string username)
        {
            // 处理系统管理员
            if (_sysAdminHandler.IsSysAdmin(username))
            {
                return await _sysAdminHandler.GetSysAdminUserAsync(username);
            }

            // 获取普通用户
            var user = await _authRepository.GetByUsernameAsync(username);
            if (user == null || user.Status != CommonStatus.Enabled)
            {
                return null;
            }

            return user;
        }

        /// <summary>
        /// 检查账户锁定状态
        /// </summary>
        private (bool IsLocked, string ErrorMessage) CheckAccountLockout(UserModel user)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                var lockoutEndTime = user.LockoutEnd.Value.ToString("yyyy-MM-dd HH:mm:ss");
                return (true, $"账户已锁定至 {lockoutEndTime}");
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// 验证密码 - 简化版本
        /// </summary>
        private async Task<(bool IsValid, string ErrorMessage)> ValidatePasswordAsync(UserModel user, string password)
        {
            string storedHash;

            if (_sysAdminHandler.IsSysAdmin(user.Username))
            {
                storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync() ?? string.Empty;
            }
            else
            {
                storedHash = user.PasswordHash;
            }

            if (string.IsNullOrEmpty(storedHash))
            {
                return (false, "用户密码未设置");
            }

            var verifyResult = PasswordHelper.Verify(storedHash, password);
            if (!verifyResult)
            {
                return (false, "密码错误");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 处理登录失败
        /// </summary>
        private async Task HandleFailedLoginAsync(UserModel user, LoginRequest dto, string reason)
        {
            // 增加失败次数
            user.FailedLoginCount++;

            // 检查是否需要锁定账户
            if (user.FailedLoginCount >= _authOptions.MaxFailedLoginAttempts)
            {
                user.LockoutEnd = DateTime.Now.Add(_authOptions.AccountLockoutDuration);
            }

            // 更新用户锁定信息（仅对非sysadmin用户）
            if (!_sysAdminHandler.IsSysAdmin(user.Username))
            {
                await _authRepository.UpdateUserLoginProtectionAsync(user);
            }

            var message = $"{reason}，失败次数: {user.FailedLoginCount}";
            if (user.LockoutEnd.HasValue)
            {
                message += $"，账户已锁定至: {user.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss}";
            }

            await LogFailedLogin(user.Id, user.RealName, message, dto);
        }

        /// <summary>
        /// 处理登录成功
        /// </summary>
        private async Task<UserDto> HandleSuccessfulLoginAsync(UserModel user, LoginRequest dto)
        {
            // 重置失败计数和锁定状态
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            user.LastLoginTime = DateTime.Now;

            // 更新数据库（仅对非sysadmin用户）
            if (!_sysAdminHandler.IsSysAdmin(user.Username))
            {
                await _authRepository.UpdateLastLoginTimeAsync(user.Id, user.LastLoginTime.Value);
                await _authRepository.UpdateUserLoginProtectionAsync(user);
            }

            // 记录成功日志
            await LogSuccessfulLogin(user, dto);

            // 手动创建UserDto以避免AutoMapper问题（特别是对于临时sysadmin用户）
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                RealName = user.RealName,
                Status = user.Status,
                CreateTime = user.CreateTime,
                LastLoginTime = user.LastLoginTime,
                PhoneNumber = user.PhoneNumber
            };
        }

        /// <summary>
        /// 记录登录失败日志
        /// </summary>
        private async Task LogFailedLogin(Guid userId, string operatorName, string reason, LoginRequest dto)
        {
            if (!_authOptions.EnableDetailedLoginLogging)
                return;

            var content = $"登录失败: {reason}";
            if (!string.IsNullOrEmpty(dto.ClientIp))
            {
                content += $" | IP: {dto.ClientIp}";
            }
            if (!string.IsNullOrEmpty(dto.UserAgent))
            {
                content += $" | UA: {dto.UserAgent}";
            }

            await LogUserAction(userId, operatorName, ActionType.Login, content);
        }

        /// <summary>
        /// 记录登录成功日志
        /// </summary>
        private async Task LogSuccessfulLogin(UserModel user, LoginRequest dto)
        {
            if (!_authOptions.EnableDetailedLoginLogging)
                return;

            var content = "登录成功";
            if (!string.IsNullOrEmpty(dto.ClientIp))
            {
                content += $" | IP: {dto.ClientIp}";
            }
            if (!string.IsNullOrEmpty(dto.UserAgent))
            {
                content += $" | UA: {dto.UserAgent}";
            }

            await LogUserAction(user.Id, user.RealName, ActionType.Login, content);
        }

        /// <summary>
        /// 记录登录异常日志
        /// </summary>
        private async Task LogLoginException(string username, Exception ex, LoginRequest dto)
        {
            var content = $"登录异常: {ex.Message}";
            if (!string.IsNullOrEmpty(dto.ClientIp))
            {
                content += $" | IP: {dto.ClientIp}";
            }

            await LogUserAction(Guid.Empty, username, ActionType.Login, content);
        }

        /// <summary>
        /// 统一的用户操作日志记录 - 简化为ILogger
        /// </summary>
        private async Task LogUserAction(Guid userId, string operatorName, ActionType actionType, string content)
        {
            _logger.LogInformation("认证操作日志 - 操作者: {OperatorName} ({UserId}), 操作类型: {ActionType}, 内容: {Content}",
                operatorName, userId, actionType, content);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取操作人姓名
        /// </summary>
        private string GetOperatorName(UserModel? user, string fallbackUsername)
        {
            if (!string.IsNullOrEmpty(user?.RealName))
            {
                return user.RealName;
            }

            if (_sysAdminHandler.IsSysAdmin(fallbackUsername))
            {
                return "系统管理员";
            }

            return fallbackUsername;
        }

        #endregion 私有辅助方法
    }
}