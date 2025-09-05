using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options
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
        public HttpsOptions Https { get; set; } = new();

        /// <summary>
        /// CORS配置
        /// </summary>
        public CorsOptions Cors { get; set; } = new();

        /// <summary>
        /// 安全头配置
        /// </summary>
        public SecurityHeadersOptions SecurityHeaders { get; set; } = new();

        /// <summary>
        /// 密码策略配置
        /// </summary>
        public PasswordPolicyOptions PasswordPolicy { get; set; } = new();

        /// <summary>
        /// 限流配置
        /// </summary>
        public RateLimitOptions RateLimit { get; set; } = new();

        /// <summary>
        /// 环境配置
        /// </summary>
        public EnvironmentOptions Environment { get; set; } = new();
    }

    /// <summary>
    /// HTTPS配置
    /// </summary>
    public class HttpsOptions
    {
        /// <summary>
        /// 是否要求HTTPS
        /// </summary>
        public bool RequireHttps { get; set; } = false;

        /// <summary>
        /// HSTS最大存活天数
        /// </summary>
        [Range(0, 3650, ErrorMessage = "HSTS最大存活天数必须在0-3650之间")]
        public int HstsMaxAgeDays { get; set; } = 365;

        /// <summary>
        /// 是否包含子域名
        /// </summary>
        public bool HstsIncludeSubdomains { get; set; } = true;

        /// <summary>
        /// 是否预加载
        /// </summary>
        public bool HstsPreload { get; set; } = true;
    }

    /// <summary>
    /// CORS配置
    /// </summary>
    public class CorsOptions
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
        /// 预检请求最大缓存时间（秒）
        /// </summary>
        [Range(0, 86400, ErrorMessage = "预检请求最大缓存时间必须在0-86400秒之间")]
        public int PreflightMaxAge { get; set; } = 3600;
    }

    /// <summary>
    /// 安全头配置
    /// </summary>
    public class SecurityHeadersOptions
    {
        /// <summary>
        /// 内容安全策略
        /// </summary>
        public string ContentSecurityPolicy { get; set; } = "default-src 'self'";

        /// <summary>
        /// X-Frame-Options
        /// </summary>
        public string XFrameOptions { get; set; } = "SAMEORIGIN";

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
        public string PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=()";
    }

    /// <summary>
    /// 密码策略配置
    /// </summary>
    public class PasswordPolicyOptions
    {
        /// <summary>
        /// 最小长度
        /// </summary>
        [Range(6, 128, ErrorMessage = "密码最小长度必须在6-128之间")]
        public int MinLength { get; set; } = 8;

        /// <summary>
        /// 是否要求大写字母
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// 是否要求小写字母
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// 是否要求数字
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// 是否要求特殊字符
        /// </summary>
        public bool RequireSpecialChar { get; set; } = true;

        /// <summary>
        /// 禁止的密码模式
        /// </summary>
        public List<string> ForbiddenPatterns { get; set; } = new() { "password", "123456", "qwerty", "admin" };

        /// <summary>
        /// 密码历史记录数量
        /// </summary>
        [Range(0, 20, ErrorMessage = "密码历史记录数量必须在0-20之间")]
        public int PasswordHistoryCount { get; set; } = 5;

        /// <summary>
        /// 密码过期天数
        /// </summary>
        [Range(0, 365, ErrorMessage = "密码过期天数必须在0-365之间")]
        public int PasswordExpiryDays { get; set; } = 90;
    }

    /// <summary>
    /// 限流配置
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 一般请求限制
        /// </summary>
        public RateLimitRule General { get; set; } = new()
        {
            RequestsPerMinute = 100,
            RequestsPerHour = 1000,
            RequestsPerDay = 10000
        };

        /// <summary>
        /// 认证请求限制
        /// </summary>
        public RateLimitRule Authentication { get; set; } = new()
        {
            RequestsPerMinute = 5,
            RequestsPerHour = 20,
            RequestsPerDay = 100
        };

        /// <summary>
        /// API密钥请求限制
        /// </summary>
        public RateLimitRule ApiKey { get; set; } = new()
        {
            RequestsPerMinute = 300,
            RequestsPerHour = 5000,
            RequestsPerDay = 50000
        };
    }

    /// <summary>
    /// 限流规则
    /// </summary>
    public class RateLimitRule
    {
        /// <summary>
        /// 每分钟请求数
        /// </summary>
        [Range(1, 10000, ErrorMessage = "每分钟请求数必须在1-10000之间")]
        public int RequestsPerMinute { get; set; } = 60;

        /// <summary>
        /// 每小时请求数
        /// </summary>
        [Range(1, 100000, ErrorMessage = "每小时请求数必须在1-100000之间")]
        public int RequestsPerHour { get; set; } = 1000;

        /// <summary>
        /// 每天请求数
        /// </summary>
        [Range(1, 1000000, ErrorMessage = "每天请求数必须在1-1000000之间")]
        public int RequestsPerDay { get; set; } = 10000;
    }

    /// <summary>
    /// 环境配置
    /// </summary>
    public class EnvironmentOptions
    {
        /// <summary>
        /// 是否隐藏服务器信息
        /// </summary>
        public bool HideServerInfo { get; set; } = false;

        /// <summary>
        /// 是否隐藏详细错误
        /// </summary>
        public bool HideDetailedErrors { get; set; } = false;

        /// <summary>
        /// 是否启用敏感数据日志记录
        /// </summary>
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// 允许的主机
        /// </summary>
        public List<string> AllowedHosts { get; set; } = new() { "localhost", "127.0.0.1" };

        /// <summary>
        /// 信任的代理
        /// </summary>
        public List<string> TrustedProxies { get; set; } = new() { "127.0.0.1", "::1" };
    }
}
