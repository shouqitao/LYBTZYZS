using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Options
{
    /// <summary>
    /// 安全配置选项
    /// </summary>
    public class SecurityOptions
    {
        public const string SectionName = "Security";

        /// <summary>
        /// HTTPS配置
        /// </summary>
        public HttpsConfiguration Https { get; set; } = new();

        /// <summary>
        /// CORS配置
        /// </summary>
        public CorsConfiguration Cors { get; set; } = new();

        /// <summary>
        /// 安全头配置
        /// </summary>
        public SecurityHeadersConfiguration SecurityHeaders { get; set; } = new();

        /// <summary>
        /// 密码策略配置
        /// </summary>
        public PasswordPolicyConfiguration PasswordPolicy { get; set; } = new();

        /// <summary>
        /// API限流配置
        /// </summary>
        public RateLimitConfiguration RateLimit { get; set; } = new();

        /// <summary>
        /// 环境安全配置
        /// </summary>
        public EnvironmentSecurityConfiguration Environment { get; set; } = new();
    }

    /// <summary>
    /// HTTPS配置
    /// </summary>
    public class HttpsConfiguration
    {
        /// <summary>
        /// 强制HTTPS重定向
        /// </summary>
        public bool RequireHttps { get; set; } = true;

        /// <summary>
        /// HSTS最大年龄（天）
        /// </summary>
        [Range(1, 365)]
        public int HstsMaxAgeDays { get; set; } = 365;

        /// <summary>
        /// 是否包含子域
        /// </summary>
        public bool HstsIncludeSubdomains { get; set; } = true;

        /// <summary>
        /// 是否预加载HSTS
        /// </summary>
        public bool HstsPreload { get; set; } = true;
    }

    /// <summary>
    /// CORS配置
    /// </summary>
    public class CorsConfiguration
    {
        /// <summary>
        /// 允许的源
        /// </summary>
        public List<string> AllowedOrigins { get; set; } = new();

        /// <summary>
        /// 允许的方法
        /// </summary>
        public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };

        /// <summary>
        /// 允许的头
        /// </summary>
        public List<string> AllowedHeaders { get; set; } = new() { "Content-Type", "Authorization", "X-Requested-With", "Accept", "Origin" };

        /// <summary>
        /// 是否允许凭据
        /// </summary>
        public bool AllowCredentials { get; set; } = true;

        /// <summary>
        /// 预检请求缓存时间（秒）
        /// </summary>
        [Range(0, 86400)]
        public int PreflightMaxAge { get; set; } = 3600;
    }

    /// <summary>
    /// 安全头配置
    /// </summary>
    public class SecurityHeadersConfiguration
    {
        /// <summary>
        /// 内容安全策略
        /// </summary>
        public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; base-uri 'self'; form-action 'self'";

        /// <summary>
        /// X-Frame-Options
        /// </summary>
        public string XFrameOptions { get; set; } = "DENY";

        /// <summary>
        /// X-Content-Type-Options
        /// </summary>
        public string XContentTypeOptions { get; set; } = "nosniff";

        /// <summary>
        /// Referrer-Policy
        /// </summary>
        public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

        /// <summary>
        /// Permissions-Policy
        /// </summary>
        public string PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=(), interest-cohort=()";
    }

    /// <summary>
    /// 密码策略配置
    /// </summary>
    public class PasswordPolicyConfiguration
    {
        /// <summary>
        /// 最小长度
        /// </summary>
        [Range(8, 128)]
        public int MinLength { get; set; } = 12;

        /// <summary>
        /// 需要大写字母
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// 需要小写字母
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// 需要数字
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// 需要特殊字符
        /// </summary>
        public bool RequireSpecialChar { get; set; } = true;

        /// <summary>
        /// 禁止的常见密码模式
        /// </summary>
        public List<string> ForbiddenPatterns { get; set; } = new()
        {
            "password",
            "123456",
            "qwerty",
            "admin",
            "user",
            "test"
        };

        /// <summary>
        /// 密码历史记录数量
        /// </summary>
        [Range(1, 24)]
        public int PasswordHistoryCount { get; set; } = 5;

        /// <summary>
        /// 密码过期天数（0表示不过期）
        /// </summary>
        [Range(0, 365)]
        public int PasswordExpiryDays { get; set; } = 90;
    }

    /// <summary>
    /// API限流配置
    /// </summary>
    public class RateLimitConfiguration
    {
        /// <summary>
        /// 是否启用限流
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 通用限流策略
        /// </summary>
        public RateLimitPolicy General { get; set; } = new()
        {
            RequestsPerMinute = 100,
            RequestsPerHour = 1000,
            RequestsPerDay = 10000
        };

        /// <summary>
        /// 登录接口限流
        /// </summary>
        public RateLimitPolicy Authentication { get; set; } = new()
        {
            RequestsPerMinute = 5,
            RequestsPerHour = 20,
            RequestsPerDay = 100
        };

        /// <summary>
        /// API键限流
        /// </summary>
        public RateLimitPolicy ApiKey { get; set; } = new()
        {
            RequestsPerMinute = 300,
            RequestsPerHour = 5000,
            RequestsPerDay = 50000
        };
    }

    /// <summary>
    /// 限流策略
    /// </summary>
    public class RateLimitPolicy
    {
        /// <summary>
        /// 每分钟请求数
        /// </summary>
        [Range(1, 10000)]
        public int RequestsPerMinute { get; set; }

        /// <summary>
        /// 每小时请求数
        /// </summary>
        [Range(1, 100000)]
        public int RequestsPerHour { get; set; }

        /// <summary>
        /// 每天请求数
        /// </summary>
        [Range(1, 1000000)]
        public int RequestsPerDay { get; set; }
    }

    /// <summary>
    /// 环境安全配置
    /// </summary>
    public class EnvironmentSecurityConfiguration
    {
        /// <summary>
        /// 是否隐藏服务器信息
        /// </summary>
        public bool HideServerInfo { get; set; } = true;

        /// <summary>
        /// 是否在生产环境中禁用详细错误信息
        /// </summary>
        public bool HideDetailedErrors { get; set; } = true;

        /// <summary>
        /// 是否启用敏感数据日志记录
        /// </summary>
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// 允许的主机列表
        /// </summary>
        public List<string> AllowedHosts { get; set; } = new();

        /// <summary>
        /// 信任的代理地址
        /// </summary>
        public List<string> TrustedProxies { get; set; } = new();
    }
}