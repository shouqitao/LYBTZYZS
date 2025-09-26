using System.ComponentModel.DataAnnotations;

namespace LYBT.Core.Infrastructure.Configuration.Options
{
    /// <summary>
    /// 用户模块配置选项
    /// </summary>
    public class UserOptions
    {
        public const string SectionName = "UserOptions";


        /// <summary>
        /// 是否启用用户信息缓存
        /// </summary>
        public bool EnableUserCache { get; set; } = true;

        /// <summary>
        /// 用户缓存过期时间（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "用户缓存过期时间必须在1-1440分钟之间")]
        public int UserCacheExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 批量操作的最大数量限制
        /// </summary>
        [Range(1, 1000, ErrorMessage = "批量操作最大数量必须在1-1000之间")]
        public int MaxBatchOperationSize { get; set; } = 100;

        /// <summary>
        /// 是否启用详细操作日志
        /// </summary>
        public bool EnableDetailedAuditLogging { get; set; } = true;

        /// <summary>
        /// 密码重置后是否发送通知
        /// </summary>
        public bool SendPasswordResetNotification { get; set; } = false;

        /// <summary>
        /// 用户会话超时时间（分钟）
        /// </summary>
        [Range(5, 1440, ErrorMessage = "用户会话超时时间必须在5-1440分钟之间")]
        public int SessionTimeoutMinutes { get; set; } = 480;

        /// <summary>
        /// 是否启用用户在线状态跟踪
        /// </summary>
        public bool EnableOnlineStatusTracking { get; set; } = true;
    }
}
