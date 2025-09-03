namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 安全配置管理服务接口
    /// </summary>
    public interface ISecurityConfigurationService
    {
        /// <summary>
        /// 获取安全配置
        /// </summary>
        SecurityConfiguration GetSecurityConfiguration();

        /// <summary>
        /// 更新安全配置
        /// </summary>
        Task UpdateSecurityConfigurationAsync(SecurityConfiguration configuration);

        /// <summary>
        /// 获取密码策略
        /// </summary>
        PasswordPolicy GetPasswordPolicy();

        /// <summary>
        /// 获取JWT配置
        /// </summary>
        EnhancedJwtOptions GetJwtOptions();

        /// <summary>
        /// 获取限流配置 (已移除过度设计的RateLimitOptions)
        /// </summary>
        // RateLimitOptions GetRateLimitOptions();

        /// <summary>
        /// 获取输入验证配置
        /// </summary>
        InputValidationOptions GetInputValidationOptions();

        /// <summary>
        /// 检查功能是否启用
        /// </summary>
        bool IsFeatureEnabled(string featureName);

        /// <summary>
        /// 获取加密密钥
        /// </summary>
        string GetEncryptionKey(string keyName);

        /// <summary>
        /// 设置加密密钥
        /// </summary>
        Task SetEncryptionKeyAsync(string keyName, string key);

        /// <summary>
        /// 轮换加密密钥
        /// </summary>
        Task<string> RotateEncryptionKeyAsync(string keyName);

        /// <summary>
        /// 获取安全头部配置
        /// </summary>
        SecurityHeadersOptions GetSecurityHeadersOptions();
    }

    /// <summary>
    /// 安全配置
    /// </summary>
    public class SecurityConfiguration
    {
        public PasswordPolicy PasswordPolicy { get; set; } = new();
        public EnhancedJwtOptions JwtOptions { get; set; } = new();
        // public RateLimitOptions RateLimitOptions { get; set; } = new(); // 已移除过度设计的限流功能
        public InputValidationOptions InputValidationOptions { get; set; } = new();
        public SecurityHeadersOptions SecurityHeadersOptions { get; set; } = new();
        public Dictionary<string, string> EncryptionKeys { get; set; } = new();
        public Dictionary<string, bool> FeatureFlags { get; set; } = new();
        public AuditConfiguration AuditConfiguration { get; set; } = new();
        public SessionConfiguration SessionConfiguration { get; set; } = new();
    }

    /// <summary>
    /// 密码策略
    /// </summary>
    public class PasswordPolicy
    {
        public int MinimumLength { get; set; } = 8;
        public int MaximumLength { get; set; } = 128;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;
        public int MaxFailedAttempts { get; set; } = 5;
        public int LockoutMinutes { get; set; } = 30;
        public int PasswordHistoryCount { get; set; } = 5; // 禁止重复使用最近5个密码
        public int PasswordExpiryDays { get; set; } = 90; // 密码90天过期
        public bool AllowPasswordReset { get; set; } = true;
        public List<string> ProhibitedPasswords { get; set; } = new()
        {
            "password", "123456", "admin", "root", "guest"
        };
    }

    /// <summary>
    /// 安全头部选项
    /// </summary>
    public class SecurityHeadersOptions
    {
        public string XFrameOptions { get; set; } = "DENY";
        public string ContentSecurityPolicy { get; set; } = 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
        public string PermissionsPolicy { get; set; } = 
            "geolocation=(), microphone=(), camera=(), fullscreen=(self)";
        public bool EnableHSTS { get; set; } = true;
        public int HSTSMaxAge { get; set; } = 31536000; // 1年
        public bool HSTSIncludeSubDomains { get; set; } = true;
        public bool HSTSPreload { get; set; } = false;
        public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
        public bool RemoveServerHeader { get; set; } = true;
        public bool RemovePoweredByHeader { get; set; } = true;
    }

    /// <summary>
    /// 审计配置
    /// </summary>
    public class AuditConfiguration
    {
        public bool EnableLoginAudit { get; set; } = true;
        public bool EnableApiAccessAudit { get; set; } = true;
        public bool EnableDataAccessAudit { get; set; } = true;
        public bool EnableSecurityExceptionAudit { get; set; } = true;
        public int AuditRetentionDays { get; set; } = 365;
        public List<string> SensitiveOperations { get; set; } = new()
        {
            "DELETE", "UPDATE", "INSERT", "BACKUP", "RESTORE"
        };
        public List<string> HighRiskEndpoints { get; set; } = new()
        {
            "/api/v1/users/delete",
            "/api/v1/database/backup",
            "/api/v1/system/config"
        };
    }

    /// <summary>
    /// 会话配置
    /// </summary>
    public class SessionConfiguration
    {
        public int SessionTimeoutMinutes { get; set; } = 480; // 8小时
        public int ExtendedSessionTimeoutMinutes { get; set; } = 43200; // 30天
        public bool AllowConcurrentSessions { get; set; } = false;
        public int MaxConcurrentSessions { get; set; } = 3;
        public bool RequireSecureConnection { get; set; } = true;
        public bool ValidateSessionIP { get; set; } = true;
        public bool EnableSessionActivityLogging { get; set; } = true;
    }
}