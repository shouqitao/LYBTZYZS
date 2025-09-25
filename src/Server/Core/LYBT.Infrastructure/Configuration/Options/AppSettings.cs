using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options
{
    /// <summary>
    /// 统一应用配置类 - 简化版本
    /// 合并原来17+个Options类为3个主要配置类
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 数据库配置
        /// </summary>
        public DatabaseSettings Database { get; set; } = new();

        /// <summary>
        /// 安全配置
        /// </summary>
        public SecuritySettings Security { get; set; } = new();

        /// <summary>
        /// 业务配置
        /// </summary>
        public BusinessSettings Business { get; set; } = new();
    }

    /// <summary>
    /// 数据库配置 - 合并原 DatabaseOptions、ConnectionPoolOptions、DatabaseMonitoringOptions、DatabaseBackupOptions
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用自动迁移
        /// </summary>
        public bool EnableAutoMigration { get; set; } = false;

        /// <summary>
        /// 命令超时时间（秒）
        /// </summary>
        [Range(1, 300)]
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// 最大连接数
        /// </summary>
        [Range(10, 1000)]
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// 最小连接数
        /// </summary>
        [Range(0, 100)]
        public int MinPoolSize { get; set; } = 0;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;

        /// <summary>
        /// 是否启用自动备份
        /// </summary>
        public bool EnableAutoBackup { get; set; } = true;

        /// <summary>
        /// 备份路径
        /// </summary>
        public string BackupPath { get; set; } = "./Backups";

        /// <summary>
        /// 备份保留天数
        /// </summary>
        [Range(1, 365)]
        public int BackupRetentionDays { get; set; } = 30;
    }

    /// <summary>
    /// 安全配置 - 合并原 AuthOptions、JwtOptions、SecurityOptions、RateLimitingOptions、SysAdminOptions、PasswordPolicyOptions
    /// </summary>
    public class SecuritySettings
    {
        /// <summary>
        /// JWT配置
        /// </summary>
        public JwtSettings Jwt { get; set; } = new();

        /// <summary>
        /// 密码策略
        /// </summary>
        public PasswordPolicySettings PasswordPolicy { get; set; } = new();

        /// <summary>
        /// 速率限制配置
        /// </summary>
        public RateLimitSettings RateLimit { get; set; } = new();

        /// <summary>
        /// 系统管理员配置
        /// </summary>
        public SysAdminSettings SysAdmin { get; set; } = new();

        /// <summary>
        /// 是否启用HTTPS
        /// </summary>
        public bool RequireHttps { get; set; } = true;

        /// <summary>
        /// 跨域配置
        /// </summary>
        public string[] AllowedOrigins { get; set; } = ["http://localhost:3000"];
    }

    /// <summary>
    /// JWT配置
    /// </summary>
    public class JwtSettings
    {
        [Required]
        public string SecretKey { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = "LYBT.WebAPI";

        [Required]
        public string Audience { get; set; } = "LYBT.Client";

        /// <summary>
        /// 令牌有效期（分钟）
        /// </summary>
        [Range(1, 10080)]
        public int TokenExpirationMinutes { get; set; } = 480; // 8小时

        /// <summary>
        /// 记住我令牌有效期（天）
        /// </summary>
        [Range(1, 365)]
        public int RememberMeExpirationDays { get; set; } = 30;
    }

    /// <summary>
    /// 密码策略配置
    /// </summary>
    public class PasswordPolicySettings
    {
        /// <summary>
        /// 最小长度
        /// </summary>
        [Range(6, 128)]
        public int MinLength { get; set; } = 8;

        /// <summary>
        /// 要求数字
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// 要求小写字母
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// 要求大写字母
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// 要求特殊字符
        /// </summary>
        public bool RequireNonAlphanumeric { get; set; } = false;

        /// <summary>
        /// 默认密码模板
        /// </summary>
        public string DefaultPasswordTemplate { get; set; } = "{username}@2025";
    }

    /// <summary>
    /// 速率限制配置
    /// </summary>
    public class RateLimitSettings
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 窗口期（秒）
        /// </summary>
        [Range(1, 3600)]
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// 最大请求数
        /// </summary>
        [Range(1, 10000)]
        public int MaxRequests { get; set; } = 100;

        /// <summary>
        /// 白名单IP
        /// </summary>
        public string[] WhitelistedIPs { get; set; } = ["::1", "127.0.0.1"];
    }

    /// <summary>
    /// 系统管理员配置
    /// </summary>
    public class SysAdminSettings
    {
        /// <summary>
        /// 默认用户名
        /// </summary>
        [Required]
        public string Username { get; set; } = "sysadmin";

        /// <summary>
        /// 默认密码（仅首次初始化使用）
        /// </summary>
        [Required]
        public string DefaultPassword { get; set; } = "LybtAdmin2025@SecurePass!";

        /// <summary>
        /// 是否自动创建
        /// </summary>
        public bool AutoCreate { get; set; } = true;
    }

    /// <summary>
    /// 业务配置 - 合并原 UserOptions、DefaultPasswordOptions、CacheOptions、SessionOptions
    /// </summary>
    public class BusinessSettings
    {
        /// <summary>
        /// 用户配置
        /// </summary>
        public UserSettings User { get; set; } = new();

        /// <summary>
        /// 缓存配置
        /// </summary>
        public CacheSettings Cache { get; set; } = new();

        /// <summary>
        /// 会话配置
        /// </summary>
        public SessionSettings Session { get; set; } = new();

        /// <summary>
        /// 诊所信息
        /// </summary>
        public ClinicSettings Clinic { get; set; } = new();
    }

    /// <summary>
    /// 用户配置
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// 最小用户名长度
        /// </summary>
        [Range(3, 50)]
        public int MinUsernameLength { get; set; } = 3;

        /// <summary>
        /// 最大用户名长度
        /// </summary>
        [Range(3, 50)]
        public int MaxUsernameLength { get; set; } = 30;

        /// <summary>
        /// 默认每页大小
        /// </summary>
        [Range(5, 100)]
        public int DefaultPageSize { get; set; } = 20;

        /// <summary>
        /// 最大导出记录数
        /// </summary>
        [Range(100, 10000)]
        public int MaxExportRecords { get; set; } = 1000;
    }

    /// <summary>
    /// 缓存配置
    /// </summary>
    public class CacheSettings
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 默认过期时间（分钟）
        /// </summary>
        [Range(1, 1440)]
        public int DefaultExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// 滑动过期时间（分钟）
        /// </summary>
        [Range(1, 1440)]
        public int SlidingExpirationMinutes { get; set; } = 20;
    }

    /// <summary>
    /// 会话配置
    /// </summary>
    public class SessionSettings
    {
        /// <summary>
        /// 会话超时时间（分钟）
        /// </summary>
        [Range(1, 1440)]
        public int TimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// 最大并发会话数
        /// </summary>
        [Range(1, 100)]
        public int MaxConcurrentSessions { get; set; } = 5;

        /// <summary>
        /// 是否允许多设备登录
        /// </summary>
        public bool AllowMultipleDevices { get; set; } = false;
    }

    /// <summary>
    /// 诊所信息配置
    /// </summary>
    public class ClinicSettings
    {
        /// <summary>
        /// 诊所名称
        /// </summary>
        [Required]
        public string Name { get; set; } = "凌隐宝堂中医诊所";

        /// <summary>
        /// 诊所地址
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 联系电话
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 营业时间
        /// </summary>
        public string BusinessHours { get; set; } = "周一至周六 8:00-18:00";

        /// <summary>
        /// 版权信息
        /// </summary>
        public string Copyright { get; set; } = "© 2025 凌隐宝堂中医诊所";
    }
}