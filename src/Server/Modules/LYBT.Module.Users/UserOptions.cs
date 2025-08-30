using LYBT.Infrastructure.Options;
namespace LYBT.Module.Users
{

    /// <summary>
    /// 用户模块的配置项
    /// </summary>
    public class UserOptions
    {

        /// <summary>
        /// 新建用户的默认密码
        /// </summary>
        public string DefaultUserPassword { get; set; } = "ChangeMe123";

        /// <summary>
        /// 是否启用用户信息缓存
        /// </summary>
        public bool EnableUserCache { get; set; } = true;

        /// <summary>
        /// 用户缓存过期时间（分钟）
        /// </summary>
        public int UserCacheExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 批量操作的最大数量限制
        /// </summary>
        public int MaxBatchOperationSize { get; set; } = 100;

        /// <summary>
        /// 是否启用详细操作日志
        /// </summary>
        public bool EnableDetailedAuditLogging { get; set; } = true;

        /// <summary>
        /// 密码重置后是否发送通知
        /// </summary>
        public bool SendPasswordResetNotification { get; set; } = false;
    }
}