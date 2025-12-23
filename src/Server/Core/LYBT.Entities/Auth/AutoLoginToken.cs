using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.Auth
{
    /// <summary>
    /// AutoLoginToken实体 - 用于"记住密码"功能的自动登录
    /// OpenSpec: refactor-login-authentication (CVT-001)
    /// </summary>
    /// <remarks>
    /// <para>功能: 存储服务端生成的自动登录令牌，替代在客户端存储密码</para>
    /// <para>安全: Token可随时撤销，支持Token轮换，设备绑定</para>
    /// <para>生命周期: 默认30天有效，每次使用后轮换生成新Token</para>
    /// </remarks>
    public class AutoLoginToken : BaseEntity
    {
        /// <summary>
        /// 令牌值（加密安全随机字符串）
        /// </summary>
        [Required]
        [StringLength(512)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 关联的用户ID
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户名（冗余存储，用于快速查询和日志）
        /// </summary>
        [Required]
        [StringLength(32)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 是否已撤销
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// 撤销原因
        /// </summary>
        [StringLength(256)]
        public string? RevokedReason { get; set; }

        /// <summary>
        /// 撤销时间
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// 撤销者
        /// </summary>
        [StringLength(128)]
        public string? RevokedBy { get; set; }

        /// <summary>
        /// 是否已使用（用于Token轮换检测）
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// 使用时间
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// 被替换的令牌（用于Token轮换）
        /// </summary>
        [StringLength(512)]
        public string? ReplacedByToken { get; set; }

        /// <summary>
        /// 设备ID（用于设备绑定）
        /// </summary>
        [StringLength(128)]
        public string? DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [StringLength(200)]
        public string? DeviceName { get; set; }

        /// <summary>
        /// 客户端IP地址
        /// </summary>
        [StringLength(45)]
        public string? ClientIp { get; set; }

        /// <summary>
        /// 用户代理字符串
        /// </summary>
        [StringLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 使用次数（用于监控异常使用）
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Token家族ID（用于检测重放攻击）
        /// </summary>
        [StringLength(128)]
        public string? FamilyId { get; set; }

        /// <summary>
        /// 是否激活（计算属性）
        /// </summary>
        [NotMapped]
        public bool IsActive => !IsRevoked && !IsDeleted && !IsUsed && ExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// 检查Token是否有效
        /// </summary>
        public bool IsValid()
        {
            return !IsRevoked &&
                   !IsDeleted &&
                   !IsUsed &&
                   ExpiresAt > DateTime.UtcNow;
        }

        /// <summary>
        /// 撤销Token
        /// </summary>
        public void Revoke(string reason, string? revokedBy = null)
        {
            IsRevoked = true;
            RevokedReason = reason;
            RevokedAt = DateTime.UtcNow;
            RevokedBy = revokedBy ?? "System";
        }

        /// <summary>
        /// 记录使用
        /// </summary>
        public void RecordUsage()
        {
            UsageCount++;
            LastUsedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 标记为已使用（用于Token轮换）
        /// </summary>
        public void MarkAsUsed(string? replacedByToken = null)
        {
            IsUsed = true;
            UsedAt = DateTime.UtcNow;
            ReplacedByToken = replacedByToken;
        }

        /// <summary>
        /// 检测是否为重放攻击
        /// </summary>
        public bool IsReplayAttack => IsUsed;
    }
}
