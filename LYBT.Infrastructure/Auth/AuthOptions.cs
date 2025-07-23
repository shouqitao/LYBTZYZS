namespace LYBT.Infrastructure.Auth {

    /// <summary>
    /// 认证模块配置选项
    /// </summary>
    public class AuthOptions {
        /// <summary>
        /// 最大登录失败次数，超过后锁定账户
        /// </summary>
        public int MaxFailedLoginAttempts { get; set; } = 5;

        /// <summary>
        /// 账户锁定持续时间
        /// </summary>
        public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 系统管理员默认密码
        /// </summary>
        public string DefaultSysAdminPassword { get; set; } = "Admin123!";

        /// <summary>
        /// 是否启用详细登录日志
        /// </summary>
        public bool EnableDetailedLoginLogging { get; set; } = true;

        /// <summary>
        /// 支持的登录类型
        /// </summary>
        public List<string> SupportedLoginTypes { get; set; } = new() { "Password" };
    }
}