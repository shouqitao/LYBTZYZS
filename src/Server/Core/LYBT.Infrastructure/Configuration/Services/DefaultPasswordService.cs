using LYBT.Shared.Configuration.Options.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Configuration.Services
{
    /// <summary>
    /// 默认密码管理服务
    /// 实现环境感知的默认密码治理：Dev-only 保护 + 单点逻辑
    /// unify-configuration-system: 迁移到 LYBT.Shared.Configuration
    /// </summary>
    /// <remarks>
    /// 核心治理规则:
    /// 1. 生产环境：强制禁用所有默认密码功能
    /// 2. 开发环境：可选启用默认密码以便调试
    /// 3. 单点逻辑：统一从 DefaultPasswordOptions 获取所有默认密码
    /// </remarks>
    public class DefaultPasswordService
    {
        private readonly DefaultPasswordOptions _options;
        private readonly IWebHostEnvironment _environment;

        public DefaultPasswordService(IOptions<DefaultPasswordOptions> defaultPasswordOptions, IWebHostEnvironment environment)
        {
            // unify-configuration-system: 使用强类型 DefaultPasswordOptions
            _options = defaultPasswordOptions.Value;
            _environment = environment;
        }

        /// <summary>
        /// 获取系统管理员默认密码 - 环境感知
        /// </summary>
        /// <returns>管理员默认密码，如果不允许使用则返回null</returns>
        public string? GetSystemAdminPassword()
        {
            // 检查环境保护规则
            if (!IsDefaultPasswordAllowed())
            {
                return null;
            }

            return _options.SysAdminPassword;
        }

        /// <summary>
        /// 获取新用户默认密码 - 环境感知
        /// </summary>
        /// <returns>新用户默认密码，如果不允许使用则返回null</returns>
        public string? GetNewUserPassword()
        {
            // 检查环境保护规则
            if (!IsDefaultPasswordAllowed())
            {
                return null;
            }

            return _options.NewUserPassword;
        }

        /// <summary>
        /// 检查是否允许使用默认密码
        /// </summary>
        /// <returns>true表示允许，false表示禁止</returns>
        public bool IsDefaultPasswordAllowed()
        {
            // 生产环境检查：强制禁用
            if (_environment.IsProduction())
            {
                // 生产环境严格禁止默认密码
                return false;
            }

            // 开发环境检查：根据配置决定
            if (_environment.IsDevelopment())
            {
                return _options.EnableInDevelopment;
            }

            // 其他环境（如Staging）：保守策略，默认禁用
            return false;
        }

        /// <summary>
        /// 检查默认密码是否仅在数据库为空时可用
        /// </summary>
        /// <param name="isDatabaseEmpty">数据库是否为空</param>
        /// <returns>true表示可用，false表示不可用</returns>
        public bool IsDefaultPasswordAvailable(bool isDatabaseEmpty)
        {
            // 基础环境检查
            if (!IsDefaultPasswordAllowed())
            {
                return false;
            }

            // 如果配置要求仅在数据库为空时可用
            if (_options.OnlyWhenDatabaseEmpty)
            {
                return isDatabaseEmpty;
            }

            return true;
        }

        /// <summary>
        /// 获取默认密码配置摘要（用于日志和监控）
        /// </summary>
        public DefaultPasswordSummary GetConfigurationSummary()
        {
            return new DefaultPasswordSummary
            {
                IsProduction = _environment.IsProduction(),
                IsDevelopment = _environment.IsDevelopment(),
                IsDefaultPasswordAllowed = IsDefaultPasswordAllowed(),
                EnableInDevelopment = _options.EnableInDevelopment,
                AllowInProduction = _options.AllowInProduction,
                OnlyWhenDatabaseEmpty = _options.OnlyWhenDatabaseEmpty,
                ExpiryDays = _options.ExpiryDays
            };
        }
    }

    /// <summary>
    /// 默认密码配置摘要
    /// </summary>
    public class DefaultPasswordSummary
    {
        public bool IsProduction { get; set; }
        public bool IsDevelopment { get; set; }
        public bool IsDefaultPasswordAllowed { get; set; }
        public bool EnableInDevelopment { get; set; }
        public bool AllowInProduction { get; set; }
        public bool OnlyWhenDatabaseEmpty { get; set; }
        public int ExpiryDays { get; set; }
    }
}
