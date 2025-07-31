using LYBT.Shared.Models.Enums;
using LYBT.Models.Users;
using LYBT.Module.Auth.Interfaces;

namespace LYBT.Module.Auth.Services {

    /// <summary>
    /// 系统管理员特殊处理器
    /// </summary>
    public class SysAdminHandler {
        private readonly IAuthRepository _authRepository;
        private const string SYSADMIN_USERNAME = "sysadmin";

        public SysAdminHandler(IAuthRepository authRepository) {
            _authRepository = authRepository;
        }

        /// <summary>
        /// 判断是否为系统管理员用户名
        /// </summary>
        public bool IsSysAdmin(string username) {
            return string.Equals(username, SYSADMIN_USERNAME, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取系统管理员用户（如果不存在则创建临时用户）
        /// </summary>
        public async Task<UserModel?> GetSysAdminUserAsync(string username) {
            if (!IsSysAdmin(username)) {
                return null;
            }

            // 尝试从用户表获取
            var user = await _authRepository.GetByUsernameAsync(username);

            // 如果不存在，创建临时内存用户
            if (user == null) {
                user = CreateTempSysAdminUser();
            }

            // 确保sysadmin始终具有管理员角色
            EnsureAdminRole(user);

            return user;
        }

        /// <summary>
        /// 获取系统管理员密码哈希
        /// </summary>
        public async Task<string?> GetSysAdminPasswordHashAsync() {
            return await _authRepository.GetAdminPasswordHashAsync(SYSADMIN_USERNAME);
        }

        /// <summary>
        /// 创建临时的系统管理员用户对象
        /// </summary>
        private UserModel CreateTempSysAdminUser() {
            return new UserModel {
                Id = Guid.NewGuid(),
                UserName = SYSADMIN_USERNAME,
                RealName = "系统管理员",
                PinyinCode = "XTGLY",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedTime = DateTime.Now,
                PasswordHash = string.Empty // 密码从AdminSecrets表获取
            };
        }

        /// <summary>
        /// 确保用户具有管理员角色
        /// </summary>
        private void EnsureAdminRole(UserModel user) {
            user.Role = UserRole.Admin;
        }
    }
}