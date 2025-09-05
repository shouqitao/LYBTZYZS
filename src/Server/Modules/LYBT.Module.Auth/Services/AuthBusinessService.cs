using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证业务服务 - UltraThink架构
    /// 职责：登录流程、密码验证、业务逻辑处理
    /// </summary>
    public class AuthBusinessService : IAuthBusinessService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IAuthQueryService _queryService;
        private readonly IJwtAuthenticationService _jwtAuthenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthBusinessService> _logger;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthBusinessService(
            IAuthRepository authRepository,
            IAuthQueryService queryService,
            IJwtAuthenticationService jwtAuthenticationService,
            IMapper mapper,
            ILogger<AuthBusinessService> logger,
            SysAdminHandler sysAdminHandler)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _jwtAuthenticationService = jwtAuthenticationService ?? throw new ArgumentNullException(nameof(jwtAuthenticationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
        }

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
                var userResult = await _queryService.GetUserForAuthenticationAsync(request.Username);
                if (!userResult.IsSuccess || userResult.Data == null)
                {
                    _logger.LogWarning("用户不存在: {Username}", request.Username);
                    return ServiceResult<LoginResponse>.Failure("用户名或密码错误");
                }

                var user = userResult.Data;

                // 2.5. 检查账户是否被锁定
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var remainingLockoutTime = user.LockoutEnd.Value.Subtract(DateTime.UtcNow);
                    _logger.LogWarning("用户账户被锁定: {Username}, 锁定到期时间: {LockoutEnd}", request.Username, user.LockoutEnd.Value);
                    return ServiceResult<LoginResponse>.Failure($"账户已被锁定，请在 {Math.Ceiling(remainingLockoutTime.TotalMinutes)} 分钟后重试");
                }

                // 3. 验证密码
                var passwordResult = await ValidatePasswordAsync(user, request.Password);
                if (!passwordResult.IsSuccess || !passwordResult.Data)
                {
                    // 3.1. 密码验证失败，增加失败次数
                    await IncrementFailedLoginCountAsync(user);
                    _logger.LogWarning("密码验证失败: {Username}, 当前失败次数: {FailedCount}", request.Username, user.FailedLoginCount + 1);
                    return ServiceResult<LoginResponse>.Failure("用户名或密码错误");
                }

                // 3.2. 密码验证成功，重置失败计数
                await ResetFailedLoginCountAsync(user);

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

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
        {
            var userResult = await _queryService.GetUserForAuthenticationAsync(request.Username);
            if (!userResult.IsSuccess)
            {
                return ServiceResult<string>.Failure(userResult.ErrorMessage ?? "获取用户信息失败");
            }

            var passwordResult = await ValidatePasswordAsync(userResult.Data!, request.Password);
            if (!passwordResult.IsSuccess || !passwordResult.Data)
            {
                return ServiceResult<string>.Failure("用户名或密码错误");
            }

            return ServiceResult<string>.Success("凭据验证成功");
        }

        #region 私有辅助方法

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

        /// <summary>
        /// 增加失败登录计数并检查是否需要锁定账户
        /// </summary>
        private async Task IncrementFailedLoginCountAsync(User user)
        {
            const int maxFailedAttempts = 5; // 最大失败尝试次数
            const int lockoutMinutes = 30;   // 锁定时间（分钟）

            user.FailedLoginCount++;

            // 如果达到最大失败次数，锁定账户
            if (user.FailedLoginCount >= maxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                _logger.LogWarning("用户账户已锁定: {Username}, 失败次数: {FailedCount}, 锁定到期时间: {LockoutEnd}",
                    user.Username, user.FailedLoginCount, user.LockoutEnd);
            }

            await _authRepository.UpdateUserSecurityAsync(user.Id, user.FailedLoginCount, user.LockoutEnd);
        }

        /// <summary>
        /// 重置失败登录计数
        /// </summary>
        private async Task ResetFailedLoginCountAsync(User user)
        {
            if (user.FailedLoginCount > 0 || user.LockoutEnd.HasValue)
            {
                user.FailedLoginCount = 0;
                user.LockoutEnd = null;
                await _authRepository.UpdateFailedLoginInfoAsync(user.Id, 0, null);
                _logger.LogInformation("用户登录成功，重置失败计数: {Username}", user.Username);
            }
        }

        #endregion
    }
}
