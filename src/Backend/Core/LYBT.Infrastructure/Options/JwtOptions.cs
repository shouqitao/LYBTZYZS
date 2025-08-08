namespace LYBT.Infrastructure.Options
{

    /// <summary>
    /// JWT 配置项
    /// </summary>
    public class JwtOptions
    {

        /// <summary>
        /// 密钥
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// 发行者
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// 受众
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间（分钟）
        /// </summary>
        public int ExpireMinutes { get; set; } = 60;

        /// <summary>
        /// "记住我"过期时间（分钟）- 默认30天
        /// </summary>
        public int RememberMeExpireMinutes { get; set; } = 43200; // 30天 = 30 * 24 * 60

        /// <summary>
        /// 是否启用刷新令牌
        /// </summary>
        public bool EnableRefreshToken { get; set; } = true;

        /// <summary>
        /// 刷新令牌过期时间（天）
        /// </summary>
        public int RefreshTokenExpireDays { get; set; } = 7;

        /// <summary>
        /// 时钟偏移容忍度（秒）
        /// </summary>
        public int ClockSkewSeconds { get; set; } = 300;
    }
}