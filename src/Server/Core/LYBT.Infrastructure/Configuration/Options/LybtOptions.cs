using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options;

/// <summary>
/// 凌隐宝堂系统统一配置选项
/// 整合所有分散的配置类为统一结构，简化配置管理
/// </summary>
public class LybtOptions
{
    public const string SectionName = "Lybt";

    /// <summary>
    /// 认证相关配置
    /// 整合：AuthOptions, JwtOptions, DefaultPasswordOptions
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new();

    /// <summary>
    /// 安全相关配置
    /// 整合：SecurityOptions, RateLimitingOptions
    /// </summary>
    public SecurityOptions Security { get; set; } = new();

    /// <summary>
    /// 基础设施配置
    /// 保留：DatabaseOptions, CacheOptions
    /// </summary>
    public InfrastructureOptions Infrastructure { get; set; } = new();

    /// <summary>
    /// 业务逻辑配置
    /// 整合：UserOptions, SysAdminOptions
    /// </summary>
    public DomainOptions Business { get; set; } = new();

    /// <summary>
    /// 应用层配置
    /// 整合：WebApiConfigurationOptions
    /// </summary>
    public ApplicationOptions Application { get; set; } = new();
}

/// <summary>
/// 认证相关配置选项
/// 整合原 AuthOptions, JwtOptions, DefaultPasswordOptions
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// JWT 配置
    /// </summary>
    public JwtConfiguration Jwt { get; set; } = new();

    /// <summary>
    /// 密码策略配置
    /// </summary>
    public PasswordPolicyConfiguration PasswordPolicy { get; set; } = new();

    /// <summary>
    /// 会话配置
    /// </summary>
    public SessionConfiguration Session { get; set; } = new();

    /// <summary>
    /// 默认密码配置
    /// </summary>
    public DefaultPasswordConfiguration DefaultPasswords { get; set; } = new();
}

/// <summary>
/// JWT 配置
/// </summary>
/// <summary>
/// JWT 配置
/// </summary>
public class JwtConfiguration
{
    /// <summary>
    /// JWT 密钥
    /// </summary>
    [Required, MinLength(32)]
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// 发行者
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "LYBT.WebAPI";

    /// <summary>
    /// 接收者
    /// </summary>
    [Required]
    public string Audience { get; set; } = "LYBT.Client";

    /// <summary>
    /// 访问令牌过期时间（分钟）
    /// 安全最佳实践：15分钟短期令牌，降低令牌泄露风险
    /// </summary>
    [Range(5, 60)] // 5分钟到1小时，符合OWASP JWT安全建议
    public int AccessTokenExpirationMinutes { get; set; } = 15; // 15分钟

    /// <summary>
    /// 刷新令牌过期时间（天）
    /// 安全最佳实践：7天，平衡安全性与用户体验
    /// </summary>
    [Range(1, 30)] // 1天到30天
    public int RefreshTokenExpirationDays { get; set; } = 7; // 7天

    /// <summary>
    /// 记住我模式过期时间（天）
    /// </summary>
    [Range(7, 90)] // 限制为最多90天
    public int RememberMeExpirationDays { get; set; } = 30;

    /// <summary>
    /// 是否允许令牌刷新
    /// </summary>
    public bool AllowRefresh { get; set; } = true;

    /// <summary>
    /// 刷新令牌在过期前多少分钟可以刷新
    /// </summary>
    [Range(5, 60)] // 5分钟到1小时
    public int RefreshTokenValidityMinutes { get; set; } = 30;

    /// <summary>
    /// 密钥最小长度要求（位）
    /// 符合NIST SP 800-131A建议的256位密钥强度
    /// </summary>
    [Range(256, 512)]
    public int MinKeyLengthBits { get; set; } = 256;

    /// <summary>
    /// JWT时钟偏差容忍度（秒）
    /// </summary>
    [Range(0, 600)] // 0到10分钟
    public int ClockSkewSeconds { get; set; } = 300; // 5分钟
}

/// <summary>
/// 密码策略配置
/// MVP阶段：仅保留基础密码强度验证，移除未实现的历史/过期功能
/// Issue #1732 Phase 1: 移除PasswordHistory/Expiration未实现配置
/// </summary>
public class PasswordPolicyConfiguration
{
    /// <summary>
    /// 最小长度
    /// </summary>
    [Range(4, 100)]
    public int MinLength { get; set; } = 8;

    /// <summary>
    /// 最大长度
    /// </summary>
    [Range(8, 255)]
    public int MaxLength { get; set; } = 100;

    /// <summary>
    /// 是否要求数字
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// 是否要求小写字母
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// 是否要求大写字母
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// 是否要求特殊字符
    /// </summary>
    public bool RequireSpecialChar { get; set; } = true;

    /// <summary>
    /// 最小唯一字符数
    /// </summary>
    [Range(1, 50)]
    public int MinUniqueChars { get; set; } = 4;

    /// <summary>
    /// 允许的特殊字符
    /// </summary>
    public string AllowedSpecialChars { get; set; } = "!@#$%^&*()_+-=[]{}|;:,.<>?";
}

/// <summary>
/// 会话配置
/// </summary>
public class SessionConfiguration
{
    /// <summary>
    /// 会话超时时间（分钟）
    /// </summary>
    [Range(1, 1440)] // 1分钟到24小时
    public int TimeoutMinutes { get; set; } = 120; // 2小时

    /// <summary>
    /// 是否允许并发会话
    /// </summary>
    public bool AllowConcurrentSessions { get; set; } = false;

    /// <summary>
    /// 最大并发会话数
    /// </summary>
    [Range(1, 10)]
    public int MaxConcurrentSessions { get; set; } = 1;

    /// <summary>
    /// 记住用户名选项
    /// </summary>
    public bool AllowRememberUsername { get; set; } = true;

    /// <summary>
    /// Cookie 名称
    /// </summary>
    public string CookieName { get; set; } = ".LYBT.Auth";

    /// <summary>
    /// Cookie 过期时间（分钟）
    /// </summary>
    [Range(1, 43200)] // 1分钟到30天
    public int CookieExpirationMinutes { get; set; } = 480; // 8小时

    /// <summary>
    /// 是否启用滑动过期
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;
}

/// <summary>
/// 默认密码配置
/// </summary>
public class DefaultPasswordConfiguration
{
    /// <summary>
    /// 系统管理员默认密码
    /// </summary>
    [Required, MinLength(8)]
    public string SysAdminPassword { get; set; } = "LybtAdmin2025@SecurePass!";

    /// <summary>
    /// 新用户默认密码
    /// </summary>
    [Required, MinLength(8)]
    public string NewUserPassword { get; set; } = "Lybt2025@TempPass!";

    /// <summary>
    /// 是否强制首次登录修改密码
    /// </summary>
    public bool ForceChangeOnFirstLogin { get; set; } = true;

    /// <summary>
    /// 开发环境是否启用默认密码功能
    /// </summary>
    public bool EnableInDevelopment { get; set; } = true;

    /// <summary>
    /// 生产环境是否允许使用默认密码（安全原因应始终为false）
    /// </summary>
    public bool AllowInProduction { get; set; } = false;

    /// <summary>
    /// 是否仅在数据库无用户时才使用默认密码
    /// </summary>
    public bool OnlyWhenDatabaseEmpty { get; set; } = true;

    /// <summary>
    /// 默认密码过期天数
    /// </summary>
    [Range(1, 365, ErrorMessage = "默认密码过期天数必须在1-365天之间")]
    public int ExpiryDays { get; set; } = 30;
}

/// <summary>
/// 安全相关配置选项
/// 整合原 SecurityOptions, RateLimitingOptions
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// HTTPS 配置
    /// </summary>
    public HttpsConfiguration Https { get; set; } = new();

    /// <summary>
    /// 安全头配置
    /// </summary>
    public SecurityHeadersConfiguration SecurityHeaders { get; set; } = new();

    /// <summary>
    /// 速率限制配置
    /// </summary>
    public RateLimitingConfiguration RateLimiting { get; set; } = new();

    /// <summary>
    /// IP 安全配置
    /// </summary>
    public IpSecurityConfiguration IpSecurity { get; set; } = new();
}

/// <summary>
/// HTTPS 配置
/// </summary>
public class HttpsConfiguration
{
    /// <summary>
    /// 是否强制HTTPS
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// HSTS 最大期限（秒）
    /// </summary>
    [Range(0, 31536000)] // 0 to 1 year
    public int HstsMaxAgeSeconds { get; set; } = 31536000; // 1 year

    /// <summary>
    /// 是否包含子域名
    /// </summary>
    public bool HstsIncludeSubdomains { get; set; } = true;

    /// <summary>
    /// 是否启用HSTS预加载
    /// </summary>
    public bool HstsPreload { get; set; } = false;
}

/// <summary>
/// 安全头配置
/// </summary>
public class SecurityHeadersConfiguration
{
    /// <summary>
    /// X-Content-Type-Options
    /// </summary>
    public string ContentTypeOptions { get; set; } = "nosniff";

    /// <summary>
    /// X-Frame-Options
    /// </summary>
    public string FrameOptions { get; set; } = "SAMEORIGIN";

    /// <summary>
    /// X-XSS-Protection
    /// </summary>
    public string XssProtection { get; set; } = "1; mode=block";

    /// <summary>
    /// Referrer-Policy
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Content-Security-Policy
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; media-src 'self'; object-src 'none'; child-src 'none'; frame-src 'none'; worker-src 'none'; frame-ancestors 'self'; form-action 'self'; upgrade-insecure-requests;";

    /// <summary>
    /// Permissions-Policy
    /// </summary>
    public string PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=(), gyroscope=(), magnetometer=(), accelerometer=(), payment=(), usb=()";
}

/// <summary>
/// 速率限制配置
/// </summary>
public class RateLimitingConfiguration
{
    /// <summary>
    /// 全局速率限制
    /// </summary>
    public RateLimitRule GlobalLimit { get; set; } = new()
    {
        PermitLimit = 1000,
        WindowSeconds = 60,
        ReplenishmentPeriodSeconds = 1
    };

    /// <summary>
    /// 登录API速率限制
    /// </summary>
    public RateLimitRule LoginLimit { get; set; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60,
        ReplenishmentPeriodSeconds = 12
    };

    /// <summary>
    /// 普通API速率限制
    /// </summary>
    public RateLimitRule ApiLimit { get; set; } = new()
    {
        PermitLimit = 100,
        WindowSeconds = 60,
        ReplenishmentPeriodSeconds = 1
    };

    /// <summary>
    /// 是否启用速率限制
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// 速率限制规则
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// 允许的请求数
    /// </summary>
    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// 时间窗口（秒）
    /// </summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// 补充周期（秒）
    /// </summary>
    [Range(1, 3600)]
    public int ReplenishmentPeriodSeconds { get; set; } = 1;

    /// <summary>
    /// 队列处理顺序
    /// </summary>
    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;
}

/// <summary>
/// 队列处理顺序
/// </summary>
public enum QueueProcessingOrder
{
    OldestFirst,
    NewestFirst
}

/// <summary>
/// IP 安全配置
/// </summary>
public class IpSecurityConfiguration
{
    /// <summary>
    /// 允许的IP地址列表
    /// </summary>
    public List<string> AllowedIpAddresses { get; set; } = new();

    /// <summary>
    /// 禁止的IP地址列表
    /// </summary>
    public List<string> BlockedIpAddresses { get; set; } = new();

    /// <summary>
    /// 是否启用IP白名单
    /// </summary>
    public bool EnableIpWhitelist { get; set; } = false;

    /// <summary>
    /// 是否启用IP黑名单
    /// </summary>
    public bool EnableIpBlacklist { get; set; } = true;

    /// <summary>
    /// 失败尝试阈值
    /// </summary>
    [Range(1, 100)]
    public int FailedAttemptsThreshold { get; set; } = 5;

    /// <summary>
    /// 锁定持续时间（分钟）
    /// </summary>
    [Range(1, 1440)]
    public int LockoutDurationMinutes { get; set; } = 30;
}

/// <summary>
/// 基础设施配置选项
/// 保留原 DatabaseOptions, CacheOptions
/// </summary>
public class InfrastructureOptions
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    public DatabaseConfiguration Database { get; set; } = new();

    /// <summary>
    /// 缓存配置
    /// </summary>
    public CacheConfiguration Cache { get; set; } = new();
}

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// 连接字符串（可选，代码有fallback链：此处 → ConnectionStrings:DefaultConnection → 环境变量）
    /// Issue #1726 Phase 4: 移除[Required]特性，支持fallback机制
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 连接池配置
    /// </summary>
    public ConnectionPoolConfiguration ConnectionPool { get; set; } = new();

    /// <summary>
    /// 监控配置
    /// </summary>
    public DatabaseMonitoringConfiguration Monitoring { get; set; } = new();

    /// <summary>
    /// 迁移配置
    /// </summary>
    public MigrationConfiguration Migration { get; set; } = new();

    /// <summary>
    /// 重试策略配置
    /// </summary>
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
}

/// <summary>
/// 连接池配置
/// </summary>
public class ConnectionPoolConfiguration
{
    /// <summary>
    /// 最大连接数
    /// </summary>
    [Range(1, 1000)]
    public int MaxConnections { get; set; } = 100;

    /// <summary>
    /// 最小连接数
    /// </summary>
    [Range(0, 100)]
    public int MinConnections { get; set; } = 5;

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    [Range(1, 3600)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 命令超时时间（秒）
    /// </summary>
    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 连接生命周期（秒）
    /// </summary>
    [Range(0, 3600)]
    public int ConnectionLifetimeSeconds { get; set; } = 0;

    /// <summary>
    /// 连接空闲超时（秒）
    /// </summary>
    [Range(60, 3600)]
    public int ConnectionIdleTimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// 数据库监控配置
/// </summary>
public class DatabaseMonitoringConfiguration
{
    /// <summary>
    /// 是否启用监控
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 慢查询阈值（毫秒）
    /// </summary>
    [Range(100, 60000)]
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// 是否记录所有查询
    /// </summary>
    public bool LogAllQueries { get; set; } = false;

    /// <summary>
    /// 是否记录参数
    /// </summary>
    public bool LogParameters { get; set; } = true;

    /// <summary>
    /// 监控统计间隔（秒）
    /// </summary>
    [Range(10, 3600)]
    public int StatisticsIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// 迁移配置
/// </summary>
public class MigrationConfiguration
{
    /// <summary>
    /// 是否自动执行迁移
    /// </summary>
    public bool AutoMigrate { get; set; } = false;

    /// <summary>
    /// 迁移超时时间（秒）
    /// </summary>
    [Range(30, 7200)]
    public int MigrationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 是否在开发环境自动创建数据库
    /// </summary>
    public bool EnsureCreatedInDevelopment { get; set; } = true;
}

/// <summary>
/// 重试策略配置
/// </summary>
public class RetryPolicyConfiguration
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 重试延迟基数（毫秒）
    /// </summary>
    [Range(100, 10000)]
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// 最大延迟时间（毫秒）
    /// </summary>
    [Range(1000, 60000)]
    public int MaxDelayMs { get; set; } = 10000;

    /// <summary>
    /// 是否启用指数退避
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}

/// <summary>
/// 缓存配置
/// MVP阶段：仅使用内存缓存，避免Redis等分布式缓存的复杂度
/// Issue #1732 Phase 1: 移除Redis相关配置
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// 内存缓存配置
    /// </summary>
    public MemoryCacheConfiguration MemoryCache { get; set; } = new();

    /// <summary>
    /// 缓存监控配置
    /// </summary>
    public CacheMonitoringConfiguration Monitoring { get; set; } = new();
}

/// <summary>
/// 内存缓存配置
/// </summary>
public class MemoryCacheConfiguration
{
    /// <summary>
    /// 缓存大小限制（字节）
    /// </summary>
    [Range(1024 * 1024, long.MaxValue)] // 最小1MB
    public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// 内存压力时压缩比例
    /// </summary>
    [Range(0.1, 0.9)]
    public double CompactionPercentage { get; set; } = 0.2; // 20%

    /// <summary>
    /// 过期扫描频率（秒）
    /// </summary>
    [Range(10, 3600)]
    public int ExpirationScanFrequencySeconds { get; set; } = 60;

    /// <summary>
    /// 默认过期时间（分钟）
    /// </summary>
    [Range(1, 1440)]
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 默认滑动过期时间（分钟）
    /// </summary>
    [Range(1, 360)]
    public int DefaultSlidingExpirationMinutes { get; set; } = 10;
}

/// <summary>
/// 缓存监控配置
/// </summary>
public class CacheMonitoringConfiguration
{
    /// <summary>
    /// 是否启用监控
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 统计间隔（秒）
    /// </summary>
    [Range(10, 3600)]
    public int StatisticsIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 是否记录缓存未命中
    /// </summary>
    public bool LogCacheMisses { get; set; } = false;

    /// <summary>
    /// 是否记录缓存命中
    /// </summary>
    public bool LogCacheHits { get; set; } = false;

    /// <summary>
    /// 低命中率阈值（百分比）
    /// </summary>
    [Range(0.1, 1.0)]
    public double LowHitRateThreshold { get; set; } = 0.5; // 50%
}

/// <summary>
/// 领域配置选项
/// 整合原 UserOptions, SysAdminOptions
/// </summary>
public class DomainOptions
{
    /// <summary>
    /// 用户管理配置
    /// </summary>
    public UserManagementConfiguration UserManagement { get; set; } = new();

    /// <summary>
    /// 系统管理员配置
    /// </summary>
    public SystemAdminConfiguration SystemAdmin { get; set; } = new();

    /// <summary>
    /// 诊疗运营配置
    /// </summary>
    public MedicalOperationsConfiguration MedicalOperations { get; set; } = new();
}

/// <summary>
/// 用户管理配置
/// </summary>
public class UserManagementConfiguration
{
    /// <summary>
    /// 默认用户角色
    /// </summary>
    public string DefaultRole { get; set; } = "Staff";

    /// <summary>
    /// 是否允许用户自助注册
    /// </summary>
    public bool AllowSelfRegistration { get; set; } = false;

    /// <summary>
    /// 新用户需要激活
    /// </summary>
    public bool RequireEmailConfirmation { get; set; } = true;

    /// <summary>
    /// 用户名最小长度
    /// </summary>
    [Range(2, 50)]
    public int UsernameMinLength { get; set; } = 3;

    /// <summary>
    /// 用户名最大长度
    /// </summary>
    [Range(3, 100)]
    public int UsernameMaxLength { get; set; } = 50;

    /// <summary>
    /// 是否允许重复邮箱
    /// </summary>
    public bool AllowDuplicateEmail { get; set; } = false;
}

/// <summary>
/// 系统管理员配置
/// </summary>
public class SystemAdminConfiguration
{
    /// <summary>
    /// 系统管理员用户名
    /// </summary>
    [Required]
    public string Username { get; set; } = "sysadmin";

    /// <summary>
    /// 系统管理员邮箱
    /// </summary>
    [Required, EmailAddress]
    public string Email { get; set; } = "admin@lybt.com";

    /// <summary>
    /// 系统管理员显示名
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = "系统管理员";

    /// <summary>
    /// 是否自动创建系统管理员
    /// </summary>
    public bool AutoCreateOnStartup { get; set; } = true;

    /// <summary>
    /// 管理员会话超时时间（分钟）
    /// </summary>
    [Range(30, 1440)]
    public int SessionTimeoutMinutes { get; set; } = 240; // 4小时
}

/// <summary>
/// 诊疗运营配置
/// </summary>
public class MedicalOperationsConfiguration
{
    /// <summary>
    /// 默认诊疗时长(分钟)
    /// </summary>
    [Range(5, 120)]
    public int DefaultConsultationDurationMinutes { get; set; } = 30;

    /// <summary>
    /// 预约提前时间(小时)
    /// </summary>
    [Range(1, 168)]
    public int MinAdvanceBookingHours { get; set; } = 2;

    /// <summary>
    /// 最大提前预约天数
    /// </summary>
    [Range(1, 365)]
    public int MaxAdvanceBookingDays { get; set; } = 30;

    /// <summary>
    /// 是否启用预约确认
    /// </summary>
    public bool RequireAppointmentConfirmation { get; set; } = true;

    /// <summary>
    /// 处方有效天数
    /// </summary>
    [Range(1, 365)]
    public int PrescriptionValidityDays { get; set; } = 30;
}

/// <summary>
/// 应用层配置选项
/// 整合原 WebApiConfigurationOptions
/// </summary>
public class ApplicationOptions
{
    /// <summary>
    /// Web API 配置
    /// </summary>
    public WebApiConfiguration WebApi { get; set; } = new();

    /// <summary>
    /// 桌面客户端配置
    /// </summary>
    public DesktopClientConfiguration DesktopClient { get; set; } = new();

    /// <summary>
    /// 日志配置
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();
}

/// <summary>
/// Web API 配置
/// </summary>
public class WebApiConfiguration
{
    /// <summary>
    /// 性能配置
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();

    /// <summary>
    /// Swagger 配置
    /// </summary>
    public SwaggerConfiguration Swagger { get; set; } = new();

    /// <summary>
    /// JSON 配置
    /// </summary>
    public JsonConfiguration Json { get; set; } = new();

    /// <summary>
    /// CORS 配置
    /// </summary>
    public CorsConfiguration Cors { get; set; } = new();
}

/// <summary>
/// 性能配置
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// 最小工作线程数
    /// </summary>
    [Range(1, 1000)]
    public int MinWorkerThreads { get; set; } = 50;

    /// <summary>
    /// 最小 IO 线程数
    /// </summary>
    [Range(1, 1000)]
    public int MinIoThreads { get; set; } = 50;

    /// <summary>
    /// 最大并发连接数
    /// </summary>
    [Range(1, 10000)]
    public int MaxConcurrentConnections { get; set; } = 100;

    /// <summary>
    /// 请求体最大字节数
    /// </summary>
    [Range(1024, 100 * 1024 * 1024)]
    public long MaxRequestBodySize { get; set; } = 30 * 1024 * 1024; // 30MB

    /// <summary>
    /// 响应缓存最大字节数
    /// </summary>
    [Range(1024, 100 * 1024 * 1024)]
    public long ResponseCacheMaxBodySize { get; set; } = 10 * 1024 * 1024; // 10MB
}

/// <summary>
/// Swagger 配置
/// </summary>
public class SwaggerConfiguration
{
    /// <summary>
    /// API 文档标题
    /// </summary>
    [Required]
    public string Title { get; set; } = "凌隐宝堂中医诊所 API";

    /// <summary>
    /// API 文档描述
    /// </summary>
    [Required]
    public string Description { get; set; } = "凌隐宝堂中医诊所 RESTful API 接口文档";

    /// <summary>
    /// 联系人姓名
    /// </summary>
    [Required]
    public string ContactName { get; set; } = "技术支持";

    /// <summary>
    /// 联系邮箱
    /// </summary>
    [EmailAddress]
    public string ContactEmail { get; set; } = "support@lybt.com";

    /// <summary>
    /// 联系 URL
    /// </summary>
    [Url]
    public string ContactUrl { get; set; } = "https://lybt.com/support";

    /// <summary>
    /// 许可证名称
    /// </summary>
    [Required]
    public string LicenseName { get; set; } = "专有许可";

    /// <summary>
    /// 许可证 URL
    /// </summary>
    [Url]
    public string LicenseUrl { get; set; } = "https://lybt.com/license";

    /// <summary>
    /// 是否启用 XML 注释
    /// </summary>
    public bool EnableXmlComments { get; set; } = true;

    /// <summary>
    /// 路由前缀
    /// </summary>
    public string RoutePrefix { get; set; } = "swagger";

    /// <summary>
    /// 文档页标题
    /// </summary>
    public string DocumentTitle { get; set; } = "凌隐宝堂中医诊所 API 文档";

    /// <summary>
    /// 是否在生产环境启用
    /// </summary>
    public bool EnableInProduction { get; set; } = false;
}

/// <summary>
/// JSON 配置
/// </summary>
public class JsonConfiguration
{
    /// <summary>
    /// 是否使用 UnsafeRelaxedJsonEscaping
    /// </summary>
    public bool UnsafeRelaxedEscaping { get; set; } = false;

    /// <summary>
    /// 属性命名策略
    /// </summary>
    public string PropertyNamingPolicy { get; set; } = "CamelCase";

    /// <summary>
    /// 是否忽略只读属性
    /// </summary>
    public bool IgnoreReadOnlyProperties { get; set; } = false;

    /// <summary>
    /// 是否允许尾随逗号
    /// </summary>
    public bool AllowTrailingCommas { get; set; } = false;
}

/// <summary>
/// CORS 配置
/// </summary>
public class CorsConfiguration
{
    /// <summary>
    /// 是否启用 CORS
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 允许的源
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new() { "https://localhost:5001" };

    /// <summary>
    /// 允许的方法
    /// </summary>
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

    /// <summary>
    /// 允许的头
    /// </summary>
    public List<string> AllowedHeaders { get; set; } = new() { "*" };

    /// <summary>
    /// 是否允许凭据
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// 预检请求缓存时间（秒）
    /// </summary>
    [Range(0, 86400)]
    public int PreflightMaxAge { get; set; } = 3600; // 1小时
}

/// <summary>
/// 桌面客户端配置
/// </summary>
public class DesktopClientConfiguration
{
    /// <summary>
    /// 默认主题
    /// </summary>
    public string DefaultTheme { get; set; } = "Light";

    /// <summary>
    /// 默认语言
    /// </summary>
    public string DefaultLanguage { get; set; } = "zh-CN";

    /// <summary>
    /// 是否启用自动更新
    /// </summary>
    public bool EnableAutoUpdate { get; set; } = true;

    /// <summary>
    /// 更新检查间隔（小时）
    /// </summary>
    [Range(1, 168)]
    public int UpdateCheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// 本地数据保留天数
    /// </summary>
    [Range(1, 365)]
    public int LocalDataRetentionDays { get; set; } = 30;
}

/// <summary>
/// 日志配置
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// 默认日志级别
    /// </summary>
    public string DefaultLevel { get; set; } = "Information";

    /// <summary>
    /// 文件日志配置
    /// </summary>
    public FileLoggingConfiguration File { get; set; } = new();

    /// <summary>
    /// 数据库日志配置
    /// </summary>
    public DatabaseLoggingConfiguration Database { get; set; } = new();

    /// <summary>
    /// 结构化日志配置
    /// </summary>
    public StructuredLoggingConfiguration Structured { get; set; } = new();
}

/// <summary>
/// 文件日志配置
/// </summary>
public class FileLoggingConfiguration
{
    /// <summary>
    /// 是否启用文件日志
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 日志文件路径
    /// </summary>
    public string Path { get; set; } = "logs/lybt-.log";

    /// <summary>
    /// 文件滚动间隔
    /// </summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>
    /// 保留文件数
    /// </summary>
    [Range(1, 365)]
    public int RetainedFileCountLimit { get; set; } = 30;

    /// <summary>
    /// 文件大小限制（字节）
    /// </summary>
    [Range(1024 * 1024, long.MaxValue)]
    public long? FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// 输出模板
    /// </summary>
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
}

/// <summary>
/// 数据库日志配置
/// </summary>
public class DatabaseLoggingConfiguration
{
    /// <summary>
    /// 是否启用数据库日志
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 批处理大小
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// 批处理间隔（秒）
    /// </summary>
    [Range(1, 60)]
    public int BatchIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 日志保留天数
    /// </summary>
    [Range(1, 365)]
    public int RetentionDays { get; set; } = 90;
}

/// <summary>
/// 结构化日志配置
/// </summary>
public class StructuredLoggingConfiguration
{
    /// <summary>
    /// 是否启用结构化日志
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否包含范围信息
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// 是否美化输出
    /// </summary>
    public bool PrettyPrint { get; set; } = false;

    /// <summary>
    /// 时间戳格式
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff zzz";
}
