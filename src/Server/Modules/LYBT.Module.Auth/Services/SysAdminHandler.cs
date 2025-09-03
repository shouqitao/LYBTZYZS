using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Services
{

    /// <summary>
    /// 系统管理员特殊处理器
    /// </summary>
    public class SysAdminHandler
    {
        private readonly IAuthRepository _authRepository;
        private const string SYSADMIN_USERNAME = "sysadmin";        public SysAdminHandler(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        /// <summary>
        /// 判断是否为系统管理员用户名
        /// </summary>
        public bool IsSysAdmin(string username)
        {
            return string.Equals(username, SYSADMIN_USERNAME, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取系统管理员用户（如果不存在则创建临时用户）
        /// </summary>
        public async Task<User?> GetSysAdminUserAsync(string username)
        {
            if (!IsSysAdmin(username))
            {
                return null;
            }

            // 尝试从用户表获取
            var user = await _authRepository.GetByUsernameAsync(username);

            // 如果不存在，创建临时内存用户
            if (user == null)
            {
                user = CreateTempSysAdminUser();
            }

            // 确保sysadmin始终具有管理员角色
            EnsureAdminRole(user);

            return user;
        }

        /// <summary>
        /// 获取系统管理员密码哈希
        /// </summary>
        public async Task<string?> GetSysAdminPasswordHashAsync()
        {
            return await _authRepository.GetAdminPasswordHashAsync(SYSADMIN_USERNAME);
        }

        /// <summary>
        /// 创建临时的系统管理员用户对象
        /// UltraThink修复：使用固定GUID确保sysadmin用户ID一致性
        /// </summary>
        private User CreateTempSysAdminUser()
        {
            // 使用固定的系统管理员GUID，确保每次登录ID一致
            var sysadminId = new Guid("00000000-0000-0000-0000-000000000001");            
            return new User
            {
                Id = sysadminId,
                Username = SYSADMIN_USERNAME,
                RealName = "系统管理员",                PinYinCode = "XTGLY",
                Status = CommonStatus.Enabled,
                // UltraThink v2.0简化：CreateTime字段已删除
                PasswordHash = string.Empty // 密码从AdminSecrets表获取
            };
        }

        /// <summary>
        /// 确保用户具有管理员角色（已移除Role字段）
        /// </summary>
        /// <summary>
        /// 确保用户具有管理员角色
        /// </summary>
        private void EnsureAdminRole(User user)
        {
            // 确保sysadmin始终具有Admin角色
            if (user.Username.Equals(SYSADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
            {
                user.Role = UserRole.Admin;
            }
        }
    }
}
