using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 系统管理员特殊处理器
    /// 重构后支持可配置的超级管理员用户名，增强安全性
    /// </summary>
    public class SysAdminHandler
    {
        private readonly IAuthRepository _authRepository;
        private readonly SysAdminOptions _sysAdminOptions; public SysAdminHandler(IAuthRepository authRepository, IOptions<SysAdminOptions> sysAdminOptions)
        {
            _authRepository = authRepository;
            _sysAdminOptions = sysAdminOptions.Value;
        }

        /// <summary>
        /// 判断是否为系统管理员用户名
        /// 重构：使用配置文件中的用户名，而非硬编码
        /// </summary>
        public bool IsSysAdmin(string username)
        {
            return string.Equals(username, _sysAdminOptions.Username, StringComparison.OrdinalIgnoreCase);
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
        /// 重构：传递用户名但实际通过固定ID查询
        /// </summary>
        public async Task<string?> GetSysAdminPasswordHashAsync()
        {
            return await _authRepository.GetAdminPasswordHashAsync(_sysAdminOptions.Username);
        }

        /// <summary>
        /// 创建临时的系统管理员用户对象
        /// 重构：使用配置的用户名，增强安全性和灵活性
        /// </summary>
        private User CreateTempSysAdminUser()
        {
            // 使用固定的系统管理员GUID，确保每次登录ID一致
            var sysadminId = new Guid("00000000-0000-0000-0000-000000000001");
            return new User
            {
                Id = sysadminId,
                Username = _sysAdminOptions.Username, // 使用配置的用户名
                RealName = "系统管理员",
                PinYinCode = "XTGLY",
                Status = CommonStatus.Enabled,

                // UltraThink v2.0简化：CreateTime字段已删除
                PasswordHash = string.Empty // 密码从AdminSecrets表获取
            };
        }

        /// <summary>
        /// 确保用户具有管理员角色
        /// 重构：使用配置的用户名判断
        /// </summary>
        private void EnsureAdminRole(User user)
        {
            // 确保配置的超级管理员始终具有Admin角色
            if (user.Username.Equals(_sysAdminOptions.Username, StringComparison.OrdinalIgnoreCase))
            {
                user.Role = UserRole.Admin;
            }
        }
    }
}
