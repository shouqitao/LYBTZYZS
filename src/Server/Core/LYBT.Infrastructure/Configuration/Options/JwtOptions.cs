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
        [Required(ErrorMessage = "JWT密钥不能为空")]
        [MinLength(32, ErrorMessage = "JWT密钥长度至少32个字符")]
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
        /// Token过期时间（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "Token过期时间必须在1-1440分钟之间")]
        public int ExpireMinutes { get; set; } = 480;

        /// <summary>
        /// 记住我Token过期时间（分钟）
        /// </summary>
        [Range(1440, 525600, ErrorMessage = "记住我Token过期时间必须在1天-1年之间")]
        public int RememberMeExpireMinutes { get; set; } = 43200; // 30天

        /// <summary>
        /// 时钟偏差秒数
        /// </summary>
        [Range(0, 3600, ErrorMessage = "时钟偏差必须在0-3600秒之间")]
        public int ClockSkewSeconds { get; set; } = 300; // 5分钟
    }
}
