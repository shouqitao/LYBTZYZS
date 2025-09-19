using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
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
        /// 获取系统管理员密码哈希
        /// 重构：传递用户名但实际通过固定ID查询
        /// </summary>
        public async Task<string?> GetSysAdminPasswordHashAsync()
        {
            return await _authRepository.GetAdminPasswordHashAsync(_sysAdminOptions.Username);
        }

    }
}
