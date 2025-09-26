using System.ComponentModel.DataAnnotations;

namespace LYBT.Core.Infrastructure.Configuration.Options
{

    /// <summary>
    /// 认证模块配置选项
    /// </summary>
    public class AuthOptions
    {
        public const string SectionName = "AuthOptions";

        /// <summary>
        /// 最大登录失败次数，超过后锁定账户
        /// </summary>
        [Range(1, 100, ErrorMessage = "最大登录失败次数必须在1-100之间")]
        public int MaxFailedLoginAttempts { get; set; } = 5;

        /// <summary>
        /// 账户锁定持续时间
        /// </summary>
        public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 是否启用详细登录日志
        /// </summary>
        public bool EnableDetailedLoginLogging { get; set; } = true;

        /// <summary>
        /// 支持的登录类型
        /// </summary>
        public List<string> SupportedLoginTypes { get; set; } = new() { "Password" };

        /// <summary>
        /// 密码复杂度要求
        /// </summary>
        public PasswordPolicy PasswordPolicy { get; set; } = new();

        /// <summary>
        /// 会话配置
        /// </summary>
        public SessionOptions SessionOptions { get; set; } = new();
    }

    /// <summary>
    /// 密码策略配置
    /// </summary>
    public class PasswordPolicy
    {

        /// <summary>
        /// 最小长度
        /// </summary>
        [Range(4, 128, ErrorMessage = "密码最小长度必须在4-128之间")]
        public int MinLength { get; set; } = 8;

        /// <summary>
        /// 是否需要大写字母
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// 是否需要小写字母
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// 是否需要数字
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// 是否需要特殊字符
        /// </summary>
        public bool RequireSpecialChar { get; set; } = true;

        /// <summary>
        /// 密码历史记录数量（防止重复使用）
        /// </summary>
        [Range(0, 50, ErrorMessage = "密码历史记录数量必须在0-50之间")]
        public int PasswordHistoryCount { get; set; } = 5;

        /// <summary>
        /// 密码过期天数（0表示永不过期）
        /// </summary>
        [Range(0, 3650, ErrorMessage = "密码过期天数必须在0-3650之间")]
        public int PasswordExpireDays { get; set; } = 90;
    }

    /// <summary>
    /// 会话选项配置
    /// </summary>
    public class SessionOptions
    {

        /// <summary>
        /// 会话超时时间（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "会话超时时间必须在1-1440分钟之间")]
        public int TimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// 是否启用滑动过期
        /// </summary>
        public bool SlidingExpiration { get; set; } = true;

        /// <summary>
        /// 是否允许并发会话
        /// </summary>
        public bool AllowConcurrentSessions { get; set; } = false;

        /// <summary>
        /// 最大并发会话数
        /// </summary>
        [Range(1, 10, ErrorMessage = "最大并发会话数必须在1-10之间")]
        public int MaxConcurrentSessions { get; set; } = 1;
    }
}
