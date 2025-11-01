using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options
{

    /// <summary>
    /// JWT配置选项
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "JwtOptions";

        /// <summary>
        /// JWT签名密钥
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// JWT签发者
        /// </summary>
        [Required(ErrorMessage = "JWT签发者不能为空")]
        public string Issuer { get; set; } = "LYBT.WebAPI";

        /// <summary>
        /// JWT受众
        /// </summary>
        [Required(ErrorMessage = "JWT受众不能为空")]
        public string Audience { get; set; } = "LYBT.Client";

        /// <summary>
        /// Access Token过期时间（分钟）- 安全建议：15分钟
        /// </summary>
        [Range(5, 60, ErrorMessage = "Access Token过期时间必须在5-60分钟之间")]
        public int ExpireMinutes { get; set; } = 15;

        /// <summary>
        /// Refresh Token过期时间（天）
        /// </summary>
        [Range(1, 30, ErrorMessage = "Refresh Token过期时间必须在1-30天之间")]
        public int RefreshTokenExpireDays { get; set; } = 7;

        /// <summary>
        /// 记住我Token过期时间（分钟）
        /// </summary>
        [Range(1440, 43200, ErrorMessage = "记住我Token过期时间必须在1-30天之间")]
        public int RememberMeExpireMinutes { get; set; } = 10080; // 7天

        /// <summary>
        /// 时钟偏差秒数
        /// </summary>
        [Range(0, 300, ErrorMessage = "时钟偏差必须在0-300秒之间")]
        public int ClockSkewSeconds { get; set; } = 60; // 1分钟

        /// <summary>
        /// 是否验证签发者
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// 是否验证受众
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// 是否验证生命周期
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// 是否验证签名密钥
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// 是否要求HTTPS
        /// </summary>
        public bool RequireHttps { get; set; } = true;

        /// <summary>
        /// 是否启用Token黑名单
        /// </summary>
        public bool EnableBlacklist { get; set; } = true;

        /// <summary>
        /// 最大并发刷新Token数（防止Token刷新攻击）
        /// </summary>
        public int MaxConcurrentRefreshTokens { get; set; } = 5;
    }
}
