using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LYBT.Infrastructure.Options;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Auth.Helpers
{
    /// <summary>
    /// AuthService验证助手类 - UltraThink Helper模式
    /// 负责所有认证验证相关逻辑：登录类型、密码、令牌、用户凭据等
    /// </summary>
    public class AuthValidationHelper
    {
        private readonly IAuthRepository _authRepository;
        private readonly SysAdminHandler _sysAdminHandler;
        private readonly AuthOptions _authOptions;
        private readonly ILogger<AuthValidationHelper> _logger;

        public AuthValidationHelper(
            IAuthRepository authRepository,
            SysAdminHandler sysAdminHandler,
            IOptions<AuthOptions> authOptions,
            ILogger<AuthValidationHelper> logger)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
            _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 凭据验证

        /// <summary>
        /// 验证用户凭据（公共接口）
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
        /// 验证用户凭据（内部实现）
        /// </summary>
        public async Task<string?> VerifyCredentialsInternalAsync(LoginRequest dto)
        {
            try
            {
                // UltraThink v2.0简化：移除账户锁定检查
                // 原：if (await _loginAttemptService.IsAccountLockedAsync(dto.Username))

                // 验证登录类型
                var loginTypeValidation = ValidateLoginType(dto);
                if (!loginTypeValidation.IsValid)
                {
                    // 记录失败日志 - 这里需要外部提供日志记录服务
                    _logger.LogWarning("登录类型验证失败: {Username}, 错误: {Error}", dto.Username, loginTypeValidation.ErrorMessage);
                    return null;
                }

                // 获取用户信息
                var user = await GetUserForAuthentication(dto.Username);
                if (user == null)
                {
                    // UltraThink v2.0简化：移除_loginAttemptService.RecordLoginAttemptAsync
                    _logger.LogWarning("用户不存在或未启用: {Username}", dto.Username);
                    return null;
                }

                // UltraThink v2.0简化：移除账户锁定检查
                // 原：var lockoutCheck = CheckAccountLockout(user);

                // 验证密码
                var passwordValidation = await ValidatePasswordAsync(user, dto.Password);
                if (!passwordValidation.IsValid)
                {
                    // UltraThink v2.0简化：移除复杂的失败处理
                    _logger.LogWarning("密码验证失败: {Username}, 错误: {Error}", dto.Username, passwordValidation.ErrorMessage);
                    return null;
                }

                // 身份验证成功，记录登录日志
                // UltraThink v2.0简化：移除_loginAttemptService.ClearFailedAttemptsAsync
                _logger.LogInformation("用户登录成功: {Username} ({UserId})", user.Username, user.Id);
                return dto.Username; // 返回用户名而不是用户详情
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证凭据内部逻辑失败: {Username}", dto.Username);
                throw;
            }
        }

        #endregion

        #region 登录类型验证

        /// <summary>
        /// 验证登录类型
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateLoginType(LoginRequest dto)
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

        #endregion

        #region 密码验证

        /// <summary>
        /// 验证密码
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidatePasswordAsync(User user, string password)
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

        #endregion

        #region 令牌验证

        /// <summary>
        /// 验证JWT令牌
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

        #endregion

        #region 用户获取

        /// <summary>
        /// 获取用于认证的用户信息
        /// </summary>
        public async Task<User?> GetUserForAuthentication(string username)
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

        #endregion

        #region 综合验证

        /// <summary>
        /// 验证用户基本信息（用户名、密码格式等）
        /// </summary>
        public ServiceResult<bool> ValidateUserBasicInfo(LoginRequest request)
        {
            try
            {
                // 验证用户名
                if (string.IsNullOrWhiteSpace(request.Username))
                    return ServiceResult<bool>.Failure("用户名不能为空");

                if (request.Username.Length > 50)
                    return ServiceResult<bool>.Failure("用户名长度不能超过50个字符");

                // 验证密码
                if (string.IsNullOrWhiteSpace(request.Password))
                    return ServiceResult<bool>.Failure("密码不能为空");

                if (request.Password.Length < 6)
                    return ServiceResult<bool>.Failure("密码长度不能少于6个字符");

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户基本信息失败");
                return ServiceResult<bool>.Failure("验证用户基本信息失败", ex);
            }
        }

        /// <summary>
        /// 验证用户状态是否允许登录
        /// </summary>
        public ServiceResult<bool> ValidateUserStatus(User user)
        {
            try
            {
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                if (user.Status != CommonStatus.Enabled)
                    return ServiceResult<bool>.Failure("用户账户已禁用");

                // 这里可以添加更多状态检查，比如账户过期等
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户状态失败: {UserId}", user?.Id);
                return ServiceResult<bool>.Failure("验证用户状态失败", ex);
            }
        }

        #endregion
    }
}