using AutoMapper;
using System.Collections.Generic;
using System.Text;
using LYBT.Common.Enums.Logs;
using LYBT.Common.Helpers;
using LYBT.Module.Auth.Dtos;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users.Models;
using LYBT.Common.Enums.Users;

namespace LYBT.Module.Auth.Services {

    /// <summary>
    /// 认证模块核心服务，实现用户登录验证、令牌生成及登录日志记录
    /// </summary>
    public class AuthService : IAuthService {
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private const int MaxFailedLoginCount = 5; // 超过5次锁定
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthService(IAuthRepository authRepository, IMapper mapper, ILogService logService) {
            _authRepository = authRepository;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>
        /// 用户登录并校验凭据，成功后返回用户信息并记录登录日志
        /// </summary>
        /// <param name="dto">登录请求参数，包含用户名、密码、客户端信息等</param>
        /// <returns>登录成功的用户信息，失败返回 null</returns>
        public async Task<UserDto?> LoginAsync(LoginRequestDto dto) {
            try {
                // 多方式认证扩展点
                if (!string.IsNullOrEmpty(dto.LoginType) && dto.LoginType != "Password") {
                    // 这里只做分支预留，后续可扩展微信、钉钉、OAuth等
                    await _logService.AddLogAsync(new LogDto {
                        LogType = LogType.Login,
                        ObjectType = ObjectType.User,
                        ObjectId = Guid.Empty,
                        ActionType = ActionType.Login,
                        OperatorId = Guid.Empty,
                        OperatorName = dto.Username,
                        Content = $"Login failed: login type {dto.LoginType} not supported | IP: {dto.ClientIp} | UA: {dto.UserAgent}",
                        LogTime = DateTime.Now
                    });
                    return null;
                }

                var user = await _authRepository.GetByUsernameAsync(dto.Username);
                bool userExistsInDb = user != null;

                // If sysadmin user record does not exist in Users table,
                // create a temporary in-memory user for authentication.
                if (user == null && dto.Username == "sysadmin") {
                    userExistsInDb = false;
                    user = new UserModel {
                        Id = Guid.NewGuid(),
                        UserName = "sysadmin",
                        RealName = "系统管理员",
                        Roles = new List<UserRole> { UserRole.Admin },
                        IsActive = true,
                        CreatedTime = DateTime.Now,
                        PasswordHash = string.Empty
                    };
                }

                // 强制保证 sysadmin 角色为 Admin
                if (user != null && user.UserName == "sysadmin") {
                    user.Roles = new List<UserRole> { UserRole.Admin };
                }

                // 账号不存在或未启用
                if (user == null || !user.IsActive) {
                    await _logService.AddLogAsync(new LogDto {
                        LogType = LogType.Login,
                        ObjectType = ObjectType.User,
                        ObjectId = user?.Id ?? Guid.Empty,
                        ActionType = ActionType.Login,
                        OperatorId = user?.Id ?? Guid.Empty,
                        OperatorName = user?.RealName ?? dto.Username,
                        Content = $"Login failed: user not found or inactive | IP: {dto.ClientIp} | UA: {dto.UserAgent}",
                        LogTime = DateTime.Now
                    });
                    return null;
                }

                // 检查账号是否被锁定
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now) {
                    await _logService.AddLogAsync(new LogDto {
                        LogType = LogType.Login,
                        ObjectType = ObjectType.User,
                        ObjectId = user.Id,
                        ActionType = ActionType.Login,
                        OperatorId = user.Id,
                        OperatorName = user.RealName,
                        Content = $"Login failed: account locked until {user.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} | IP: {dto.ClientIp} | UA: {dto.UserAgent}",
                        LogTime = DateTime.Now
                    });
                    return null;
                }

                var storedHash = user.UserName == "sysadmin"
                    ? await _authRepository.GetAdminPasswordHashAsync(user.UserName) ?? string.Empty
                    : user.PasswordHash;

                if (!PasswordHelper.Verify(storedHash, dto.Password)) {
                    // 登录失败计数+1
                    user.FailedLoginCount++;
                    if (user.FailedLoginCount >= MaxFailedLoginCount) {
                        user.LockoutEnd = DateTime.Now.Add(LockoutDuration);
                    }
                    await _authRepository.UpdateUserLoginProtectionAsync(user); // 需要实现此方法
                    await _logService.AddLogAsync(new LogDto {
                        LogType = LogType.Login,
                        ObjectType = ObjectType.User,
                        ObjectId = user.Id,
                        ActionType = ActionType.Login,
                        OperatorId = user.Id,
                        OperatorName = user.RealName,
                        Content = $"Login failed: wrong password, failed count {user.FailedLoginCount} | IP: {dto.ClientIp} | UA: {dto.UserAgent}",
                        LogTime = DateTime.Now
                    });
                    return null;
                }

                // 登录成功，重置失败次数和锁定状态
                user.FailedLoginCount = 0;
                user.LockoutEnd = null;
                user.LastLoginTime = DateTime.Now;
                if (userExistsInDb) {
                    await _authRepository.UpdateLastLoginTimeAsync(user.Id, user.LastLoginTime.Value);
                    await _authRepository.UpdateUserLoginProtectionAsync(user); // 需要实现此方法
                }

                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Login,
                    ObjectType = ObjectType.User,
                    ObjectId = user.Id,
                    ActionType = ActionType.Login,
                    OperatorId = user.Id,
                    OperatorName = user.RealName,
                    Content = $"Login success | IP: {dto.ClientIp} | UA: {dto.UserAgent}",
                    LogTime = user.LastLoginTime.Value
                });

                return _mapper.Map<UserDto>(user);
            } catch (Exception ex) {
                // 记录详细异常日志
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Login,
                    ObjectType = ObjectType.User,
                    ObjectId = Guid.Empty,
                    ActionType = ActionType.Login,
                    OperatorId = Guid.Empty,
                    OperatorName = dto.Username,
                    Content = $"Login exception: {ex.Message} {ex.StackTrace}",
                    LogTime = DateTime.Now
                });
                throw; // 保持原有500响应
            }


        }

        /// <summary>
        /// 用户登出并写入日志
        /// </summary>
        /// <param name="dto">登出请求参数，包含用户名等信息</param>
        /// <returns>登出是否成功</returns>
        public async Task<bool> LogoutAsync(LogoutRequestDto dto) {
            var user = await _authRepository.GetByUsernameAsync(dto.Username);
            string operatorName = user?.RealName;
            if (string.IsNullOrEmpty(operatorName) && dto.Username == "sysadmin")
                operatorName = "系统管理员";
            if (string.IsNullOrEmpty(operatorName))
                operatorName = dto.Username;
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Login,
                ObjectType = ObjectType.User,
                ObjectId = user?.Id ?? Guid.Empty,
                ActionType = ActionType.Logout,
                OperatorId = user?.Id ?? Guid.Empty,
                OperatorName = operatorName,
                Content = "Logout",
                LogTime = DateTime.Now
            });
            return true;
        }

        /// <summary>
        /// 修改系统管理员密码，先校验旧密码
        /// </summary>
        /// <param name="dto">修改密码请求，包含旧密码和新密码</param>
        /// <returns>修改是否成功</returns>
        public async Task<bool> ChangeSysAdminPasswordAsync(ChangeSysAdminPasswordDto dto) {
            var hash = await _authRepository.GetAdminPasswordHashAsync("sysadmin");
            if (string.IsNullOrEmpty(hash))
                return false;
            if (!PasswordHelper.Verify(hash, dto.OldPassword))
                return false;
            var newHash = PasswordHelper.Hash(dto.NewPassword);
            await _authRepository.UpdateAdminPasswordHashAsync("sysadmin", newHash);
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.User,
                ObjectId = Guid.Empty,
                ActionType = ActionType.Edit,
                OperatorId = Guid.Empty,
                OperatorName = "sysadmin",
                Content = "Change sysadmin password",
                LogTime = DateTime.Now
            });
            return true;
        }
    }
}
