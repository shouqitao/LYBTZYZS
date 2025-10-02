using System.ComponentModel.DataAnnotations;
using LYBT.Entities.Common;

namespace LYBT.Entities.Auth
{
    /// <summary>
    /// 黑名单Token实体 - 用于管理被撤销的JWT令牌
    /// </summary>
    public class BlacklistedToken : BaseEntity
    {
        /// <summary>
        /// JWT ID (jti) - 令牌的唯一标识
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string JwtId { get; set; } = string.Empty;

        /// <summary>
        /// 关联的用户ID
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// 令牌的原始过期时间
        /// </summary>
        public DateTime TokenExpiresAt { get; set; }

        /// <summary>
        /// 加入黑名单的时间
        /// </summary>
        public DateTime BlacklistedAt { get; set; }

        /// <summary>
        /// 黑名单原因
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 操作者ID（执行撤销操作的用户）
        /// </summary>
        public Guid? RevokedBy { get; set; }

        /// <summary>
        /// 客户端IP地址
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 黑名单类型
        /// </summary>
        public BlacklistType Type { get; set; }

        /// <summary>
        /// 是否应该被清理（当前时间已超过令牌原始过期时间）
        /// </summary>
        public bool ShouldBeCleanedUp()
        {
            return DateTime.UtcNow > TokenExpiresAt;
        }
    }

    /// <summary>
    /// 黑名单类型枚举
    /// </summary>
    public enum BlacklistType
    {
        /// <summary>
        /// 用户登出
        /// </summary>
        UserLogout = 0,

        /// <summary>
        /// 管理员撤销
        /// </summary>
        AdminRevoked = 1,

        /// <summary>
        /// 密码更改
        /// </summary>
        PasswordChanged = 2,

        /// <summary>
        /// 账户锁定
        /// </summary>
        AccountLocked = 3,

        /// <summary>
        /// 安全威胁
        /// </summary>
        SecurityThreat = 4,

        /// <summary>
        /// 会话超时
        /// </summary>
        SessionTimeout = 5,

        /// <summary>
        /// 其他原因
        /// </summary>
        Other = 99
    }
}
