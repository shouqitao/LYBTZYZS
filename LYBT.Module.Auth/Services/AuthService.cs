using AutoMapper;
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
    /// 登录验证服务实现
    /// </summary>
    public class AuthService : IAuthService {
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public AuthService(IAuthRepository authRepository, IMapper mapper, ILogService logService) {
            _authRepository = authRepository;
            _mapper = mapper;
            _logService = logService;
        }

        public async Task<UserDto?> LoginAsync(LoginRequestDto dto) {
            var user = await _authRepository.GetByUsernameAsync(dto.Username);

            // If sysadmin user record does not exist in Users table,
            // create a temporary in-memory user for authentication.
            if (user == null && dto.Username == "sysadmin") {
                user = new UserModel {
                    Id = Guid.NewGuid(),
                    UserName = "sysadmin",
                    RealName = "系统管理员",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedTime = DateTime.Now,
                    PasswordHash = string.Empty
                };
            }

            if (user == null || !user.IsActive) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Login,
                    ObjectType = ObjectType.User,
                    ObjectId = user?.Id ?? Guid.Empty,
                    ActionType = ActionType.Login,
                    OperatorId = user?.Id ?? Guid.Empty,
                    OperatorName = user?.RealName ?? dto.Username,
                    Content = "Login failed",
                    LogTime = DateTime.Now
                });
                return null;
            }

            var storedHash = user.UserName == "sysadmin"
                ? await _authRepository.GetAdminPasswordHashAsync(user.UserName) ?? string.Empty
                : user.PasswordHash;

            if (!PasswordHelper.Verify(storedHash, dto.Password)) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Login,
                    ObjectType = ObjectType.User,
                    ObjectId = user?.Id ?? Guid.Empty,
                    ActionType = ActionType.Login,
                    OperatorId = user?.Id ?? Guid.Empty,
                    OperatorName = user?.RealName ?? dto.Username,
                    Content = "Login failed",
                    LogTime = DateTime.Now
                });
                return null;
            }

            user.LastLoginTime = DateTime.Now;
            // Only update DB for users that actually exist there
            if (await _authRepository.GetByUsernameAsync(user.UserName) != null) {
                await _authRepository.UpdateLastLoginTimeAsync(user.Id, user.LastLoginTime.Value);
            }

            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Login,
                ObjectType = ObjectType.User,
                ObjectId = user.Id,
                ActionType = ActionType.Login,
                OperatorId = user.Id,
                OperatorName = user.RealName,
                Content = "Login success",
                LogTime = user.LastLoginTime.Value
            });

            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> LogoutAsync(LogoutRequestDto dto) {
            var user = await _authRepository.GetByUsernameAsync(dto.Username);
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Login,
                ObjectType = ObjectType.User,
                ObjectId = user?.Id ?? Guid.Empty,
                ActionType = ActionType.Logout,
                OperatorId = user?.Id ?? Guid.Empty,
                OperatorName = user?.RealName ?? dto.Username,
                Content = "Logout",
                LogTime = DateTime.Now
            });
            return true;
        }
    }
}