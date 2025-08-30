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
    /// 认证核心CRUD服务 - UltraThink架构
    /// 职责：基础认证操作、密码验证、用户状态管理
    /// </summary>
    public class AuthServiceCore
    {
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthServiceCore> _logger;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthServiceCore(
            IAuthRepository authRepository,
            IMapper mapper,
            ILogger<AuthServiceCore> logger,
            SysAdminHandler sysAdminHandler)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
        }

        /// <summary>
        /// 根据用户名获取用户信息（用于认证）
        /// </summary>
        public async Task<ServiceResult<User>> GetUserForAuthenticationAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return ServiceResult<User>.Failure("用户名不能为空");

                // 首先检查是否为系统管理员
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
                    return ServiceResult<User>.Failure("用户不存在");

                return ServiceResult<User>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户认证信息失败: {Username}", username);
                return ServiceResult<User>.Failure($"获取用户信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证用户密码
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePasswordAsync(User user, string password)
        {
            try
            {
                if (user == null)
                    return ServiceResult<bool>.Failure("用户信息不能为空");

                if (string.IsNullOrWhiteSpace(password))
                    return ServiceResult<bool>.Failure("密码不能为空");

                // 系统管理员密码验证
                if (user.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    var passwordHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                    var isValidSysAdmin = PasswordHelper.Verify(password, passwordHash ?? string.Empty);
                    return ServiceResult<bool>.Success(isValidSysAdmin);
                }

                // 普通用户密码验证
                var isValid = PasswordHelper.Verify(password, user.PasswordHash);
                return ServiceResult<bool>.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密码验证失败: {Username}", user?.Username);
                return ServiceResult<bool>.Failure("密码验证失败");
            }
        }

        /// <summary>
        /// 验证用户基本信息
        /// </summary>
        public ServiceResult<bool> ValidateUserBasicInfo(User user)
        {
            if (user == null)
                return ServiceResult<bool>.Failure("用户信息不存在");

            if (string.IsNullOrWhiteSpace(user.Username))
                return ServiceResult<bool>.Failure("用户名不能为空");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return ServiceResult<bool>.Failure("用户密码未设置");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证用户状态
        /// </summary>
        public ServiceResult<bool> ValidateUserStatus(User user)
        {
            if (user == null)
                return ServiceResult<bool>.Failure("用户信息不存在");

            // 检查用户是否被禁用
            if (user.Status == LYBT.Shared.Models.Enums.CommonStatus.Disabled)
                return ServiceResult<bool>.Failure("账户已被禁用，请联系管理员");

            // 系统管理员跳过锁定检查
            if (user.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                return ServiceResult<bool>.Success(true);

            // 检查账户锁定状态（如果User实体有这些字段）
            // if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            //     return ServiceResult<bool>.Failure($"账户已锁定至 {user.LockoutEnd:yyyy-MM-dd HH:mm:ss}");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                    return ServiceResult<bool>.Failure("新密码不能为空");

                if (newPassword.Length < 8)
                    return ServiceResult<bool>.Failure("密码长度不能少于8位");

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
        /// 记录登录失败
        /// </summary>
        public async Task<ServiceResult<bool>> RecordLoginFailureAsync(User user)
        {
            try
            {
                if (user == null)
                    return ServiceResult<bool>.Success(true); // 用户不存在时跳过记录

                // 系统管理员跳过失败记录
                if (user.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                    return ServiceResult<bool>.Success(true);

                // 这里应该更新用户的失败登录次数（如果User实体有这个字段）
                // user.FailedLoginCount++;
                // user.LastFailedLoginTime = DateTime.UtcNow;
                // await _authRepository.UpdateUserAsync(user);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录登录失败异常: {Username}", user?.Username);
                return ServiceResult<bool>.Failure("记录登录失败异常");
            }
        }

        /// <summary>
        /// 重置失败尝试次数
        /// </summary>
        public async Task<ServiceResult<bool>> ResetFailedAttemptsAsync(User user)
        {
            try
            {
                if (user == null || user.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                    return ServiceResult<bool>.Success(true);

                // 这里应该重置用户的失败登录次数（如果User实体有这个字段）
                // user.FailedLoginCount = 0;
                // user.LockoutEnd = null;
                // await _authRepository.UpdateUserAsync(user);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置失败尝试次数异常: {Username}", user?.Username);
                return ServiceResult<bool>.Failure("重置失败尝试次数异常");
            }
        }

        /// <summary>
        /// 检查账户锁定状态
        /// </summary>
        public ServiceResult<bool> CheckAccountLockout(User user)
        {
            if (user == null)
                return ServiceResult<bool>.Success(true);

            // 系统管理员不受锁定限制
            if (user.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                return ServiceResult<bool>.Success(true);

            // 检查锁定状态（如果User实体有这些字段）
            // if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            //     return ServiceResult<bool>.Failure($"账户已锁定至 {user.LockoutEnd:yyyy-MM-dd HH:mm:ss}");

            return ServiceResult<bool>.Success(true);
        }
    }
}