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
using LYBT.Module.Auth.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证模块核心服务 - UltraThink Helper模式重构版
    /// 委托具体业务逻辑给Helper类处理，提高代码组织性和可维护性
    /// 实现用户登录验证、令牌生成及登录日志记录
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AuthValidationHelper _validationHelper;
        private readonly AuthSessionHelper _sessionHelper;
        private readonly AuthLoggingHelper _loggingHelper;
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        private readonly SysAdminHandler _sysAdminHandler;
        private readonly AuthOptions _authOptions;
        private readonly ILogger<AuthService> _logger;
        private readonly IJwtAuthenticationService _jwtAuthenticationService;

        public AuthService(
            AuthValidationHelper validationHelper,
            AuthSessionHelper sessionHelper,
            AuthLoggingHelper loggingHelper,
            IAuthRepository authRepository,
            IMapper mapper,
            SysAdminHandler sysAdminHandler,
            IOptions<AuthOptions> authOptions,
            ILogger<AuthService> logger,
            IJwtAuthenticationService jwtAuthenticationService)
        {
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _sessionHelper = sessionHelper ?? throw new ArgumentNullException(nameof(sessionHelper));
            _loggingHelper = loggingHelper ?? throw new ArgumentNullException(nameof(loggingHelper));
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
            _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtAuthenticationService = jwtAuthenticationService ?? throw new ArgumentNullException(nameof(jwtAuthenticationService));
        }

        #region Shared Interface Implementation (委托给Helper模式)

        /// <summary>
        /// 用户登录（委托给ValidationHelper处理验证，SessionHelper处理会话）
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // 验证凭据
                var credentialsResult = await _validationHelper.VerifyCredentialsAsync(request);
                if (!credentialsResult.IsSuccess)
                {
                    return ServiceResult<LoginResponse>.Failure(credentialsResult.ErrorMessage ?? "登录失败", credentialsResult.Exception);
                }

                // 获取用户信息用于创建LoginResponse
                var user = await _validationHelper.GetUserForAuthentication(request.Username);
                if (user == null)
                {
                    return ServiceResult<LoginResponse>.Failure("用户不存在");
                }

                // 处理登录成功，更新用户状态
                var userDto = await HandleSuccessfulLoginAsync(user, request);

                // 生成真正的JWT Token
                var jwtToken = _jwtAuthenticationService.GenerateToken(
                    user.Id.ToString(), 
                    user.Username, 
                    user.Role, 
                    request.RememberMe);

                // 创建登录响应
                var loginResponse = new LoginResponse
                {
                    Token = jwtToken, // 使用真正的JWT Token
                    User = userDto, // UltraThink v2.0简化：直接使用UserDto，移除BaseUser转换
                    RefreshToken = Guid.NewGuid().ToString(), // Phase 7: 新增刷新令牌
                    ExpiresAt = DateTime.UtcNow.AddMinutes(request.RememberMe ? 43200 : 480) // Phase 7: 新增过期时间
                };

                return ServiceResult<LoginResponse>.Success(loginResponse);
            }
            catch (Exception ex)
            {
                await _loggingHelper.LogLoginExceptionAsync(request.Username, ex, request);
                _logger.LogError(ex, "登录过程中发生异常: {Username}", request.Username);
                return ServiceResult<LoginResponse>.Failure("登录过程中发生异常", ex);
            }
        }

        /// <summary>
        /// 用户登出（委托给SessionHelper和LoggingHelper）
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
        {
            try
            {
                return await _sessionHelper.LogoutAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出失败: {Username}", request.Username);
                return ServiceResult<bool>.Failure("登出失败", ex);
            }
        }

        /// <summary>
        /// 修改系统管理员密码（委托给ValidationHelper验证，LoggingHelper记录）
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        {
            try
            {
                var result = await ChangeSysAdminPasswordInternalAsync(request.NewPassword);
                await _loggingHelper.LogSysAdminPasswordChangeAsync(result, result ? null : "密码修改失败");
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                await _loggingHelper.LogSysAdminPasswordChangeAsync(false, ex.Message);
                _logger.LogError(ex, "修改系统管理员密码失败");
                return ServiceResult<bool>.Failure("修改系统管理员密码失败", ex);
            }
        }

        /// <summary>
        /// 验证凭据（委托给ValidationHelper）
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
        {
            return await _validationHelper.VerifyCredentialsAsync(request);
        }

        /// <summary>
        /// 刷新Token（委托给SessionHelper）
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            return await _sessionHelper.RefreshTokenAsync(refreshToken);
        }

        /// <summary>
        /// 验证Token（委托给ValidationHelper）
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            return await _validationHelper.ValidateTokenAsync(token);
        }

        /// <summary>
        /// 获取会话信息（委托给SessionHelper）
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            return await _sessionHelper.GetSessionInfoAsync(token);
        }

        #endregion

        #region 私有辅助方法（委托给Helper或直接实现）

        /// <summary>
        /// 处理登录成功逻辑
        /// </summary>
        private async Task<UserDto> HandleSuccessfulLoginAsync(User user, LoginRequest dto)
        {
            // UltraThink v2.0简化：移除复杂的锁定状态重置和最后登录时间更新
            // 原：user.FailedLoginCount = 0; user.LockoutEnd = null;
            // 原：await _authRepository.UpdateLastLoginTimeAsync(user.Id);

            // 记录成功日志
            await _loggingHelper.LogSuccessfulLoginAsync(user, dto);

            // 手动创建UserDto以避免AutoMapper问题（特别是对于临时sysadmin用户）
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                RealName = user.RealName,
                Status = user.Status,
                // CreateTime、LastLoginTime字段已删除（UltraThink v2.0简化）
                PhoneNumber = user.PhoneNumber
            };
        }

        /// <summary>
        /// 修改系统管理员密码内部实现
        /// </summary>
        private async Task<bool> ChangeSysAdminPasswordInternalAsync(string newPassword)
        {
            try
            {
                // UltraThink v2.0简化：直接使用Repository更新密码哈希
                var passwordHash = PasswordHelper.Hash(newPassword);
                await _authRepository.UpdateAdminPasswordHashAsync("sysadmin", passwordHash);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新系统管理员密码失败");
                return false;
            }
        }

        /// <summary>
        /// 获取操作员名称（辅助方法）
        /// </summary>
        #region 已废弃功能 - UltraThink精简
        /*
        // 此方法与AuthLoggingHelper.GetOperatorName重复，已删除
        private string GetOperatorName(User? user, string username)
        {
            // 功能已迁移到AuthLoggingHelper中
        }
        */
        #endregion

        #endregion
    }
}